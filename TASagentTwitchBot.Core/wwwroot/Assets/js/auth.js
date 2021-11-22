const auth = (function () {
    let authString = '';
    let isUserAuth = false;
    let isPrivAuth = false;
    let isAdminAuth = false;

    function HandleErrorResponse(response) {
        if (response.status && response.status === 401) {
            SetAuthStatus("None");
        }
    }

    function SetAuthStatus(role) {
        isUserAuth = false;
        isPrivAuth = false;
        isAdminAuth = false;

        switch (role) {
            case "None":
                break;

            case "User":
                isUserAuth = true;
                break;

            case "Privileged":
                isUserAuth = true;
                isPrivAuth = true;
                break;

            case "Admin":
                isUserAuth = true;
                isPrivAuth = true;
                isAdminAuth = true;
                break;

            default:
                console.log("Unexpected role: " + role);
                break;
        }

        $(".requireAuth").each(function () {
            $(this).prop('disabled', !isUserAuth);
        });

        $(".requirePrivAuth").each(function () {
            $(this).prop('disabled', !isPrivAuth);
        });

        $(".requireAdminAuth").each(function () {
            $(this).prop('disabled', !isAdminAuth);
        });

        $("#text-role").val(role);

        $(".userAuthGroup").each(function () {
            if (isUserAuth) {
                $(this).show();
            }
            else {
                $(this).hide();
            }
        });

        $(".privAuthGroup").each(function () {
            if (isPrivAuth) {
                $(this).show();
            }
            else {
                $(this).hide();
            }
        });

        $(".adminAuthGroup").each(function () {
            if (isAdminAuth) {
                $(this).show();
            }
            else {
                $(this).hide();
            }
        });

    }

    function Authorize(asyncCallback) {
        $.post({
            url: "/TASagentBotAPI/Auth/Authorize",
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Password: $("#input-password").val() }),
            success: async function (result) {
                authString = result.authString;
                SetAuthStatus(result.role);
                await asyncCallback(result);
            },
            error: (result) => { SetAuthStatus("None"); }
        });
    }

    const ApplyAuth = (xhr) => {
        xhr.setRequestHeader("Authorization", authString);
    }

    return {
        authString,
        ApplyAuth,
        Authorize,
        isUserAuth,
        isPrivAuth,
        isAdminAuth,
        SetAuthStatus,
        HandleErrorResponse
    };
})();