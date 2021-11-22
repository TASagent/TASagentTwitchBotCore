namespace TASagentTwitchBot.Core.Web.Middleware;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AuthRequiredAttribute : Attribute
{
    public readonly AuthDegree authDegree;

    public AuthRequiredAttribute(AuthDegree authDegree = AuthDegree.User)
    {
        this.authDegree = authDegree;
    }
}
