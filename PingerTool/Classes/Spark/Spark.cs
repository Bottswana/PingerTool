using System;
using System.Net;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PingerTool.Classes
{
    public class Spark
    {
        public readonly RoomData.Rooms SelectedCircleId;
        public readonly int WarningThreshold;
        public readonly int TimeoutThreshold;

        private const string APIURL = "https://api.ciscospark.com/v1/";
        private WebClient _Client;

        #region Initializer
        public Spark(RoomData.Rooms SelectedRoom = null, int WarningInterval = 0, int TimeoutInterval = 0)
        {
            _Client = new WebClient()
            {
                BaseAddress = APIURL
            };

            _Client.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {SparkPrivateTokens.APITOKEN}");
            WarningThreshold = WarningInterval;
            TimeoutThreshold = TimeoutInterval;
            SelectedCircleId = SelectedRoom;
        }
        #endregion Initializer

        #region Public Methods
        /// <summary>
        /// Get rooms the bot is a member of
        /// </summary>
        /// <returns>List of rooms</returns>
        public async Task<List<RoomData.Rooms>> GetRooms()
        {
            try
            {
                var ResultData = await _Client.DownloadStringTaskAsync("rooms?type=group&sortBy=created");
                if( ResultData != null )
                {
                    var RoomData = JsonConvert.DeserializeObject<RoomData>(ResultData);
                    return new List<RoomData.Rooms>(RoomData.items);
                }
            }
            catch( WebException Ex )
            {
                var ErrorInfo = new System.IO.StreamReader(Ex.Response.GetResponseStream()).ReadToEnd();
                App.GetApp().Log.Error(Ex, $"Unable to query Spark rooms api : {ErrorInfo}");
            }
            catch( Exception Ex )
            {
                App.GetApp().Log.Error(Ex, "Unable to query Spark rooms api");
            }

            return null;
        }

        /// <summary>
        /// Send a message to a group the bot is a member of
        /// </summary>
        /// <param name="Message">Message to send</param>
        /// <param name="Markdown">Markdown message to send</param>
        /// <returns>true if successful, false otherwise</returns>
        public async Task<bool> SendMessage(string Message, string Markdown)
        {
            _Client.Headers[HttpRequestHeader.ContentType] = "application/json";
            var Request = new MessageRequest() { roomId = SelectedCircleId.id, text = Message, markdown = Markdown };
            try
            {
                var ResultData = await _Client.UploadStringTaskAsync("messages", JsonConvert.SerializeObject(Request));
                if( ResultData != null )
                {
                    return true;
                }
            }
            catch( WebException Ex )
            {
                var ErrorInfo = new System.IO.StreamReader(Ex.Response.GetResponseStream()).ReadToEnd();
                App.GetApp().Log.Error(Ex, $"Unable to send message to spark api : {ErrorInfo}");
            }
            catch( Exception Ex )
            {
                App.GetApp().Log.Error(Ex, "Unable to send message to spark api");
            }

            return false;
        }
        #endregion Public Methods
    }

    #region Spark API Data Models
    public class RoomData
    {
        public class Rooms
        {
            public string id { get; set; }
            public string title { get; set; }
        }

        public Rooms[] items { get; set; }
    }

    public class MessageRequest
    {
        public string roomId { get; set; }
        public string text { get; set; }
        public string markdown { get; set; }
    }
    #endregion Spark API Data Models
}