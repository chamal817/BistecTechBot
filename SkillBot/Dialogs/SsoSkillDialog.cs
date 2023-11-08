using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System.Text;
using Newtonsoft.Json.Linq;
using BDO.Bot.BDOSkillBot.Objects;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;

namespace BDOSkillBot.Dialogs
{
    public class SsoSkillDialog : ComponentDialog
    {
        private readonly string _connectionName;
        private readonly IConfiguration _configuration;
        bool isYes = false;

        public SsoSkillDialog(IConfiguration configuration)
            : base(nameof(SsoSkillDialog))
        {
            _connectionName = configuration.GetSection("ConnectionName")?.Value;
            _configuration = configuration;
            if (string.IsNullOrWhiteSpace(_connectionName))
            {
                throw new ArgumentException("\"ConnectionName\" is not set in configuration");
            }

            AddDialog(new SsoSkillSignInDialog(_connectionName));
            AddDialog(new ChoicePrompt("ActionStepPrompt"));

            var waterfallSteps = new WaterfallStep[]
            {
                PromptActionStepAsync,
               
                HandleUserSelectionAsync,
                HandleUserReasonAsync,
                PromptFinalStepAsync

            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            return await stepContext.BeginDialogAsync(nameof(SsoSkillSignInDialog), null, cancellationToken);
        }

    
        private async Task<DialogTurnResult> PromptFinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // Restart the dialog (we will exit when the user says "end").
            return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
        }


        private async Task<DialogTurnResult> HandleUserSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var typingActivity = new Activity
            {
                Type = ActivityTypes.Typing,
                RelatesTo = stepContext.Context.Activity.GetConversationReference(),
            };
            await stepContext.Context.SendActivityAsync(typingActivity);
            var reply = stepContext.Context.Activity.CreateReply();
         
             reply.Text = "Please tell us why you need to contact Service Desk.  \n\n Eg: I need a support ticket to Integrate a VM to my Lap.";
            
            
           
            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> HandleUserReasonAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var typingActivity = new Activity
            {
                Type = ActivityTypes.Typing,
            };
            await stepContext.Context.SendActivityAsync(typingActivity);
            await ContactAgent(stepContext, stepContext.Context, cancellationToken);


            // End the dialog
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task ContactAgent(WaterfallStepContext stepContext, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var typingActivity = new Activity
                {
                    Type = ActivityTypes.Typing,
                };
                await stepContext.Context.SendActivityAsync(typingActivity);
                var userId = stepContext.Context.Activity?.From?.Id;
                var userTokenClient = stepContext.Context.TurnState.Get<UserTokenClient>();
                var token = await userTokenClient.GetUserTokenAsync(userId, _connectionName, stepContext.Context.Activity?.ChannelId, null, cancellationToken);
                var reason = stepContext.Context.Activity;

                if (token.Token != null)
                {
                        var client = new SimpleGraphClient(token.Token, _configuration);
                        var logingUser = await client.GetUserEmail();
                        await SaveToDb(logingUser.Name, logingUser.Email , reason.Text);
                }
                else
                {
                    await SaveToDb(null, null, reason.Text);
                }
                await stepContext.Context.SendActivityAsync("Thanks for contacting Bistec Tech Bot. I will connect you with one of our agents, Have a nice day.", cancellationToken: cancellationToken);

            }

            catch (Exception ex)
            {
                // Handle any exceptions that occur while starting the chat
                await turnContext.SendActivityAsync($"Error starting the chat: {ex.Message}", cancellationToken: cancellationToken);
            }
        }

       
        private async Task SaveToDb(string name , string email , string reson)
        {

            string endpointUri = _configuration.GetSection("DBEndpointUrl")?.Value;
            string primaryKey = _configuration.GetSection("DBPrimaryKey")?.Value;

            CosmosClient cosmosClient = new CosmosClient(endpointUri, primaryKey);

            // Create a new database if it doesn't exist
            string databaseName = _configuration.GetSection("DBName")?.Value;
            DatabaseResponse databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);

            // Create a new container if it doesn't exist
            string containerName = _configuration.GetSection("DBcontainerName")?.Value;
            ContainerResponse containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerName, "/Id");

            UserInfo data = new UserInfo
            {
                
                Name = name,
                Email = email,
                Reson = reson
       
            };

            ItemResponse<UserInfo> response = await containerResponse.Container.CreateItemAsync(data);
        }

    }
}
