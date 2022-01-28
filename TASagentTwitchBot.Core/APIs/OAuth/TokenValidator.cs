using System.Security.Cryptography;

namespace TASagentTwitchBot.Core.API.OAuth;

public interface ITokenValidator
{
    void SetCode(string code, string state);
    Task<bool> TryToConnect();
    void RunValidator();

    Task<bool> WaitForValidationAsync();
    Task<bool> WaitForValidationAsync(CancellationToken cancellationToken);
}

public abstract class TokenValidator : IShutdownListener, ITokenValidator, IDisposable
{
    protected readonly ICommunication communication;
    protected readonly IOAuthHandler oauthHandler;

    protected readonly ErrorHandler errorHandler;

    /// <summary>
    /// How frequently we check to see if it's time to rerun validation
    /// </summary>
    private readonly TimeSpan validationCheckInterval = new TimeSpan(hours: 0, minutes: 5, seconds: 0);

    /// <summary>
    /// How frequently we rerun validation
    /// </summary>
    private readonly TimeSpan validationInterval = new TimeSpan(hours: 0, minutes: 30, seconds: 0);
    /// <summary>
    /// How long to wait until retrying failed validation
    /// </summary>
    private readonly TimeSpan failedValidationInterval = new TimeSpan(hours: 0, minutes: 1, seconds: 0);

    private readonly TimeSpan tokenRefreshRange = new TimeSpan(hours: 1, minutes: 0, seconds: 0);

    private DateTime nextValidateTime;
    private TaskCompletionSource<(string code, string state)>? codeCallback = null;
    private readonly List<TaskCompletionSource<bool>> awaitingValidation = new List<TaskCompletionSource<bool>>();

    private Task? validationTask = null;
    private ValidationState validationState = ValidationState.NotValidated;
    private readonly static object updateLock = new object();

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();

    private bool disposedValue = false;

    protected abstract string AccessToken { get; set; }
    protected abstract string RefreshToken { get; set; }
    protected abstract string RedirectURI { get; }

    public TokenValidator(
        ApplicationManagement applicationManagement,
        ICommunication communication,
        IOAuthHandler oauthHandler,
        ErrorHandler errorHandler)
    {
        this.communication = communication;
        this.oauthHandler = oauthHandler;
        this.errorHandler = errorHandler;

        applicationManagement.RegisterShutdownListener(this);
    }

    public void SetCode(string code, string state)
    {
        if (codeCallback is null)
        {
            communication.SendWarningMessage($"Received OAuth Code when not awaiting one.");
            return;
        }

        codeCallback.SetResult((code, state));
    }

    public async Task<bool> TryToConnect()
    {
        if (await TryExistingToken())
        {
            return true;
        }
        else if (await TryTokenRefresh())
        {
            return true;
        }
        else
        {
            //We require reauthorization
            string authCode = (await GetCode())!;

            if (string.IsNullOrEmpty(authCode))
            {
                return false;
            }

            //Try to get a new Token
            TokenRequest? request = await oauthHandler.GetToken(authCode, RedirectURI);

            //Did we receive a new Access Token?
            if (request is null)
            {
                //We failed to get a new token
                return false;
            }

            AccessToken = request.AccessToken;
            RefreshToken = request.RefreshToken;

            //Update saved accessToken
            SaveChanges();

            //Does the token validate?
            return await TryValidateToken();
        }
    }

    private async Task<bool> TryToValidate()
    {
        if (await TryExistingToken())
        {
            return true;
        }
        else if (await TryTokenRefresh())
        {
            return true;
        }

        return false;
    }

    private async Task<bool> TryExistingToken()
    {
        //Do we have an AccessToken?
        if (string.IsNullOrEmpty(AccessToken))
        {
            return false;
        }

        //Does the AccessToken validate?
        return await TryValidateToken();
    }

    private async Task<bool> TryTokenRefresh()
    {
        //Do we have a RefreshToken?
        if (string.IsNullOrEmpty(RefreshToken))
        {
            //We can't try a refresh without a RefreshToken
            return false;
        }

        //Try a refresh
        TokenRefreshRequest? request = await oauthHandler.RefreshToken(RefreshToken);

        //Did we receive an Access Token?
        if (request is null)
        {
            //Refresh attempt failed
            return false;
        }

        //Update Tokens
        AccessToken = request.AccessToken;
        RefreshToken = request.RefreshToken;

        //Update saved accessToken
        SaveChanges();

        //Does the token validate?
        return await TryValidateToken();
    }

    private async Task<bool> TryValidateToken()
    {
        //Request validation of our access_token
        TokenValidationRequest? validationRequest = await oauthHandler.ValidateToken(AccessToken);

        //Was our token validated?
        if (validationRequest is null)
        {
            return false;
        }

        //Validated
        //Do we need to refresh the token anyway?
        TimeSpan remainingTime = new TimeSpan(hours: 0, minutes: 0, seconds: validationRequest.ExpiresIn);

        if (remainingTime < tokenRefreshRange)
        {
            //Force refresh
            return false;
        }

        //Token is good
        UpdateValidationState(ValidationState.Validated);
        return true;
    }

    public void ResetValidator() => nextValidateTime = DateTime.Now + validationInterval;

    public void RunValidator()
    {
        if (validationTask is not null)
        {
            return;
        }

        validationTask = Validator();
    }

    private async Task Validator()
    {
        nextValidateTime = DateTime.Now + validationInterval;
        int errorCount = 0;
        try
        {
            while (true)
            {
                if (DateTime.Now < nextValidateTime)
                {
                    await Task.Delay(validationCheckInterval, generalTokenSource.Token);
                }
                else
                {
                    if (await TryToValidate().WithCancellation(generalTokenSource.Token))
                    {
                        errorCount = 0;
                        nextValidateTime = DateTime.Now + validationInterval;
                        //ValidationState is updated to Validated in TryValidateToken
                    }
                    else
                    {
                        if (errorCount++ > 3)
                        {
                            communication.SendErrorMessage($"Error! Failed to validate! Try quitting and re-running program.");
                            UpdateValidationState(ValidationState.FailedValidation);
                            await Task.Delay(1000, generalTokenSource.Token);
                            return;
                        }

                        communication.SendWarningMessage($"Failed to validate! Retrying in 1 minute.");
                        UpdateValidationState(ValidationState.NotValidated);
                        nextValidateTime = DateTime.Now + failedValidationInterval;
                    }
                }
            }
        }
        catch (OperationCanceledException) { /* swallow */ }
        catch (Exception ex)
        {
            errorHandler.LogSystemException(ex);
        }
    }


    private async Task<string?> GetCode()
    {
        string? code = null;
        string stateString = GenerateRandomStringToken();

        SendCodeRequest(stateString);

        codeCallback = new TaskCompletionSource<(string code, string state)>();

        //Wait up to 10 minutes
        await Task.WhenAny(
            codeCallback.Task,
            Task.Delay(1000 * 60 * 10));

        if (codeCallback.Task.IsCompleted)
        {
            (string code, string state) result = codeCallback.Task.Result;

            if (result.state != stateString)
            {
                communication.SendWarningMessage($"OAuth state string did not match:  SENT \"{stateString}\"  RECEIVED \"{result.state}\"");
            }
            else
            {
                code = result.code;
            }
        }

        codeCallback = null;
        return code;
    }

    private void UpdateValidationState(ValidationState newValidationState)
    {
        if (validationState == newValidationState)
        {
            return;
        }

        lock (updateLock)
        {
            validationState = newValidationState;

            switch (validationState)
            {
                case ValidationState.NotValidated:
                    //Do nothing - We are waiting
                    break;

                case ValidationState.Validated:
                case ValidationState.FailedValidation:
                    foreach (TaskCompletionSource<bool> taskCompletionSource in awaitingValidation)
                    {
                        taskCompletionSource.SetResult(validationState == ValidationState.Validated);
                    }
                    awaitingValidation.Clear();
                    break;

                default:
                    throw new Exception($"Unexpected ValidationState: {validationState}");
            }
        }
    }

    public Task<bool> WaitForValidationAsync()
    {
        lock (updateLock)
        {
            switch (validationState)
            {
                case ValidationState.Validated: return Task.FromResult(true);
                case ValidationState.FailedValidation: return Task.FromResult(false);
                case ValidationState.NotValidated:
                    {
                        //We are waiting - Queue TaskCompletionSource
                        TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                        awaitingValidation.Add(taskCompletionSource);
                        return taskCompletionSource.Task;
                    }

                default: throw new Exception($"Unexpected ValidationState: {validationState}");
            }
        }
    }

    public Task<bool> WaitForValidationAsync(CancellationToken cancellationToken)
    {
        lock (updateLock)
        {
            switch (validationState)
            {
                case ValidationState.Validated: return Task.FromResult(true);
                case ValidationState.FailedValidation: return Task.FromResult(false);
                case ValidationState.NotValidated:
                    {
                        //We are waiting - Queue TaskCompletionSource
                        TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                        awaitingValidation.Add(taskCompletionSource);
                        cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken));
                        return taskCompletionSource.Task;
                    }

                default: throw new Exception($"Unexpected ValidationState: {validationState}");
            }
        }
    }

    private enum ValidationState
    {
        NotValidated = 0,
        Validated,
        FailedValidation
    }

    protected abstract void SendCodeRequest(string stateString);
    protected abstract void SaveChanges();

    private static string GenerateRandomStringToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(30));

    public void NotifyShuttingDown()
    {
        generalTokenSource.Cancel();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                generalTokenSource.Cancel();

                validationTask?.Wait(2_000);
                validationTask = null;

                generalTokenSource.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
