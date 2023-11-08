using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace BDORootBot.Bots
{
    public class RootBot<T> : ActivityHandler
        where T : Dialog
    {
        private readonly ConversationState _conversationState;
        private readonly Dialog _mainDialog;

        public RootBot(ConversationState conversationState, T dialog)
        {
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _mainDialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            if (turnContext.Activity.Type != ActivityTypes.ConversationUpdate)
            {
                // Run the Dialog with the Activity.
                await _mainDialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }
            else
            {
                // Let the base class handle the activity.
                await base.OnTurnAsync(turnContext, cancellationToken);
            }

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var hasGreetedUser = await GetHasGreetedUserFlag(turnContext);
            await _conversationState.ClearStateAsync(turnContext, cancellationToken);
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message and hasn't been greeted yet.
                if (member.Id != turnContext.Activity.Recipient.Id && !hasGreetedUser)
                {
                    var message = "Hello and welcome!  I'm IT Support Assistant - BOT  \n\n Please say Hi or Hello";
                    await turnContext.SendActivityAsync(MessageFactory.Text(message), cancellationToken);
                    await _mainDialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
         
                    // Set the flag for this user to true, so they won't be greeted again.
                    await SetHasGreetedUserFlag(turnContext, true);
                }
            }
        }

        private async Task<bool> GetHasGreetedUserFlag(ITurnContext turnContext)
        {
            var userStateAccessors = _conversationState.CreateProperty<Dictionary<string, bool>>("HasGreetedUser");
            var userState = await userStateAccessors.GetAsync(turnContext, () => new Dictionary<string, bool>());
            var userId = turnContext.Activity.From.Id;
            return userState.TryGetValue(userId, out var hasGreeted) && hasGreeted;
        }

        private async Task SetHasGreetedUserFlag(ITurnContext turnContext, bool value)
        {
            var userStateAccessors = _conversationState.CreateProperty<Dictionary<string, bool>>("HasGreetedUser");
            var userState = await userStateAccessors.GetAsync(turnContext, () => new Dictionary<string, bool>());
            var userId = turnContext.Activity.From.Id;

            if (userState.ContainsKey(userId))
            {
                userState[userId] = value;
            }
            else
            {
                userState.Add(userId, value);
            }

            await _conversationState.SaveChangesAsync(turnContext);
        }
    }


}
