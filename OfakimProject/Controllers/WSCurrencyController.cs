using Newtonsoft.Json;
using OfakimProject.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;

namespace OfakimProject.Controllers
{
    public class WSCurrencyController : ApiController
    {
        BL bl = new BL();

        //1.
        public HttpResponseMessage GetCurrencyValue()
        {
            if (HttpContext.Current.IsWebSocketRequest)
            {
                HttpContext.Current.AcceptWebSocketRequest(ProcessWS);
            }
            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }
        private async Task ProcessWS(AspNetWebSocketContext context)
        {
            WebSocket ws = context.WebSocket;
            var buffer = new ArraySegment<byte>(new byte[1024]);

            while (true)
            {
                new Task(async () => 
                {
                    if (ws.State != WebSocketState.Open)
                    {
                        // MUST read if we want the state to get updated...
                        var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    }
                }).Start();
               
                while (ws.State == WebSocketState.Open)
                {
                    //Do Scraping every 1 minute
                    bool changed;
                    var currencies = bl.GetCurrencies(out changed);
                    if (changed)
                    {
                        buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(currencies)));
                        await ws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    Thread.Sleep(60000);
                }
                break;
            }
        }

        //2.
        public string GetLastCurrencies()
        {
            return JsonConvert.SerializeObject(bl.GetLastCurrencies());
        }
    }
}
