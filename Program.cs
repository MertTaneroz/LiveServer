using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Testbackend.Models;

var builder =
    WebApplication.CreateBuilder(
        args
    );

var app = builder.Build();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseWebSockets();

bool spacePressed = false;

InputPayload input = new();

// Oyun dünyasýnýn durumu
float posX = 0f;

float posY = 0f;

float hareketHizi = 1f;
// Son gelen WASD input'u burada tutulur
// Oyuncu W basýlýysa burada kalýr
InputPayload currentKeys =
    new();

ConcurrentQueue<Actions> actionQueue = new();

// Son tick'in ne zaman çalýþtýðýný tutuyor
// Ýlk deðer: sunucu açýldýðý an
DateTime lastTick =
    DateTime.UtcNow;


// Tick aralýðý
// Her 30 ms'de bir oyun update olacak
TimeSpan tickRate =
    TimeSpan.FromMilliseconds(
        30
    );



app.Map(
"/ws",

async context =>
{
    if (
        context.WebSockets
        .IsWebSocketRequest
    )
    {
        var socket =
            await context
                .WebSockets
                .AcceptWebSocketAsync();


        while (socket.State == System.Net.WebSockets.WebSocketState.Open)
        {
            

            byte[] buffer = new byte[1024];

            var receiveTask = socket.ReceiveAsync(
                buffer,
                CancellationToken.None
            );

            var delayTask = Task.Delay(5);

            var completedTask = await Task.WhenAny(receiveTask, delayTask);

            if (completedTask == receiveTask)
            {
                var result = await receiveTask;

                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(
                        System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None
                    );
                    break;
                }

                string message = Encoding.UTF8.GetString(
                    buffer,
                    0,
                    result.Count
                );
                //
                try
                {
                    InputPayload? data =
                        JsonSerializer
                            .Deserialize
                            <InputPayload>(
                                message
                            );


                    if (
                        data
                        !=
                        null
                    )
                    {
                        /*
                         Input'u uygula DEMÝYORUZ

                         sadece saklýyoruz
                        */

                        currentKeys =
                            data;
                    }

                    if (data?.actions is not null)
                    {
                        actionQueue.Enqueue(data.actions);
                    }



                    while (actionQueue.TryDequeue(out var a))
                    {
                        if (a.space)
                        {
                            spacePressed = true;
                        }
                    }

                }
                catch
                {
                }
            }
            // Deðerleri bir Tuple (demet) içine koyup desen eþleþtirme yapýyoruz
            int hareketYonKontrol = new[]
                {
                    currentKeys?.keys?.w ?? false,
                    currentKeys?.keys?.a ?? false,
                    currentKeys?.keys?.s ?? false,
                    currentKeys?.keys?.d ?? false
                }
                .Count(x => x);



            //Console.WriteLine($"{spaceKontrol}  {mausKontrol}");
            // Switch kontrolünü bu sayý üzerinden yapýyoruz



            /*
             Tick zamaný geldi mi?

             þimdi - sonTick

             30 ms geçtiyse
             oyun update
            */

            if (
                DateTime.UtcNow
                -
                lastTick
                >=
                tickRate
            )
            {
                /*
                 Burada gerçek oyun çalýþýyor
                */
               

                if (hareketYonKontrol == 2)
                {


                    if (currentKeys?.keys?.w ?? false)
                    {
                        posY += hareketHizi * 0.71f;
                    }

                    if (currentKeys?.keys?.a ?? false)
                    {
                        posX -= hareketHizi * 0.71f;
                    }

                    if (currentKeys?.keys?.s ?? false)
                    {
                        posY -= hareketHizi * 0.71f;
                    }

                    if (currentKeys?.keys?.d ?? false)
                    {
                        posX += hareketHizi * 0.71f;
                    }

                }
                else
                {
                    if (
                        currentKeys?.keys?.w ?? false
                       )
                    {
                        posY = posY + (hareketHizi);
                    }

                    if (
                        currentKeys?.keys?.a ?? false
                    )
                    {
                        posX = posX - (hareketHizi);
                    }

                    if (
                        currentKeys?.keys?.s ?? false
                    )
                    {
                        posY = posY - (hareketHizi);
                    }

                    if (
                        currentKeys?.keys?.d ?? false
                    )
                    {
                        posX = posX + (hareketHizi);
                    }
                }
                    // Ýstemciye dönecek cevap
                    var response =
                        new
                        {
                            x = posX,
                            y = posY,
                            space = spacePressed
                        };

                Console.WriteLine(response);

                string json =
                    JsonSerializer
                        .Serialize(
                            response
                        );


                byte[] sendBuffer =
                    Encoding
                        .UTF8
                        .GetBytes(
                            json
                        );


                await socket
                    .SendAsync(
                        sendBuffer,

                        System
                        .Net
                        .WebSockets
                        .WebSocketMessageType
                        .Text,

                        true,

                        CancellationToken
                        .None
                    );



                /*
                 Tick tamamlandý

                 sonraki tick buradan ölçülecek
                */
                spacePressed = false;

                lastTick =
                    DateTime.UtcNow;
            }
        }
    }
});

app.Run();



public class InputPayload
{
    public Keys? keys { get; set; }
    public Actions? actions { get; set; }
}

public class Keys
{
    public bool w { get; set; }
    public bool a { get; set; }
    public bool s { get; set; }
    public bool d { get; set; }
}

public class Actions
{
    public bool space { get; set; }
    public bool leftClick { get; set; }
}