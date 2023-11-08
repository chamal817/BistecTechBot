using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace BDOSkillBot.Dialogs
{
    public class SsoSkillSignInDialog : ComponentDialog
    {
        public SsoSkillSignInDialog(string connectionName)
            : base(nameof(SsoSkillSignInDialog))
        {
            AddDialog(new OAuthPrompt(nameof(OAuthPrompt), new OAuthPromptSettings
            {
                ConnectionName = connectionName,
                Text = "To continue,please login",
                Title = "Login",
            }));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { SignInStepAsync,
               VerifyLoginAsync
            }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SignInStepAsync(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            // This prompt won't show if the user is signed in to the root using SSO.
            return await context.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> VerifyLoginAsync(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            if (!(context.Result is TokenResponse result))
            {
                await context.Context.SendActivityAsync("Login Failed Try Again Please", cancellationToken: cancellationToken);
            }
            
           
            return await context.EndDialogAsync(null, cancellationToken);
        }
    }
}
