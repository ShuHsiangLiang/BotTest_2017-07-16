using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Http.Description;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.ProjectOxford.Vision;
using Microsoft.Cognitive.LUIS;
using Bot_Application1.Model;

namespace Bot_Application1
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            Trace.TraceInformation(JsonConvert.SerializeObject(activity, Formatting.Indented));

            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity reply = activity.CreateReply();

                if (activity.Attachments.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image"))
                {
                    ImageTemplate(reply, activity.Attachments.First().ContentUrl);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                else if (activity.Text == "快速導覽")
                {
                    reply.Text = "Menu Sample";
                    reply.SuggestedActions = new SuggestedActions()
                    {
                        Actions = new List<CardAction>()
                        {
                        new CardAction(){Title = "選項一", Type = ActionTypes.ImBack, Value = "NTD" },
                        new CardAction(){Title = "選項二", Type = ActionTypes.OpenUrl, Value = "https://www.facebook.com/NTUCEP/?fref=ts"}
                        }
                    }
                    ;
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                else
                {
                    var fbData = JsonConvert.DeserializeObject<FBChannelModel>(activity.ChannelData.ToString());
                    if (fbData.postback != null && fbData.postback.payload.StartsWith("Anaylze"))
                    {
                        //vision
                        var url = fbData.postback.payload.Split('>')[1];
                        VisionServiceClient client = new VisionServiceClient("4f10752a53b3453a9b0ed4bb2b774ba6");
                        var result = await client.AnalyzeImageAsync(url, new VisualFeature[] { VisualFeature.Description });
                        reply.Text = result.Description.Captions.First().Text;
                    }
                    else
                    {
                        using (LuisClient client = new LuisClient("a37b90e5-aef5-498d-95e5-410bd223610c", "0e3c65517f8240209fb442f2c6508ed6"))
                        {
                            var result = await client.Predict(activity.Text);
                            if (result.Intents.Count() <= 0 || result.TopScoringIntent.Name != "查詢匯率")
                            {
                                reply.Text = "別亂講！";
                            }
                            else
                            {
                                var currency1 = result.Entities?.Where(x => x.Key.StartsWith("美金"))?.First().Value[0].Value;
                                // ask api
                                reply.Text = $"{currency1}價格是30.0";
                            }
                        }
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
                }
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private void ImageTemplate(Activity reply, string url)
        {
            List<Attachment> att = new List<Attachment>();
            att.Add(new HeroCard()
            {
                Title = "歡迎使用本服務！",
                Subtitle = "請選擇一個執行項目",
                Images = new List<CardImage>() { new CardImage(url) },
                Buttons = new List<CardAction>()
            {
                new CardAction(ActionTypes.PostBack, "臉部辨識", value: $"Face>{url}"),
                new CardAction(ActionTypes.PostBack, "辨識圖片", value: $"Analyze>{url}"),
                new CardAction(ActionTypes.PostBack, "配對指數", value: $"Date>{url}")
            }
            }
            .ToAttachment());
            reply.Attachments = att;
        }
        
        /*
        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                //If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }*/
    }
}