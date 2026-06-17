using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

var builder =
    WebApplication.CreateBuilder(
        args
    );

var app =
    builder.Build();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseWebSockets();


// Oyun dünyasýnýn durumu
float posX = 0f;

float posY = 0f;

float hareketHizi = 1f;
// Son gelen WASD input'u burada tutulur
// Oyuncu W basýlýysa burada kalýr
Keys currentKeys =
    new();


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


        while (
            socket.State
            ==
            System.Net.WebSockets
                .WebSocketState
                .Open
        )
        {
            // Gelen veri burada tutulacak
            byte[] buffer =
                new byte[1024];



            // Veri alma iþlemini BAÞLAT
            // Ama burada bekleme
            var receiveTask =
                socket.ReceiveAsync(
                    buffer,
                    CancellationToken.None
                );


            /*
             receiveTask:
             "birisi veri gönderirse oku"

             Task.Delay(30):
             "30 ms bekle"

             WhenAny:
             "hangisi önce biterse devam et"

             Amaç:
             input gelmese bile
             tick durmasýn
            */

            await Task.WhenAny(
                receiveTask,
                Task.Delay(5)
            );



            /*
             Eðer veri geldiyse
             input'u iþle
            */

            if (
                receiveTask
                    .IsCompleted
            )
            {
                var result =
                    receiveTask
                        .Result;


                string message =
                    Encoding
                        .UTF8
                        .GetString(
                            buffer,
                            0,
                            result.Count
                        );
                //
                try
                {
                    Keys? data =
                        JsonSerializer
                            .Deserialize
                            <Keys>(
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
                }
                catch
                {
                }
            }
            // Deðerleri bir Tuple (demet) içine koyup desen eþleþtirme yapýyoruz
            int hareketYonKontrol = new[] { currentKeys.w, currentKeys.a, currentKeys.s, currentKeys.d }.Count(x => x);

            int spaceKontrol = new[] { currentKeys.space }.Count(x => x);

            int mausKontrol = new[] { currentKeys.leftClick }.Count(x => x);

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


                    if (
                        currentKeys.w
                    )
                    {
                        posY = posY + (hareketHizi * 0.71f);
                    }

                    if (
                        currentKeys.a
                    )
                    {
                        posX = posX - (hareketHizi * 0.71f);
                    }

                    if (
                        currentKeys.s
                    )
                    {
                        posY = posY - (hareketHizi * 0.71f);
                    }

                    if (
                        currentKeys.d
                    )
                    {
                        posX = posX + (hareketHizi * 0.71f);
                    }


                }
                else
                {
                    if (
                        currentKeys.w
                       )
                    {
                        posY = posY + (hareketHizi);
                    }

                    if (
                        currentKeys.a
                    )
                    {
                        posX = posX - (hareketHizi);
                    }

                    if (
                        currentKeys.s
                    )
                    {
                        posY = posY - (hareketHizi);
                    }

                    if (
                        currentKeys.d
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
                            y = posY
                        };


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

                lastTick =
                    DateTime.UtcNow;
            }
        }
    }
});

app.Run();



class Keys
{
    public bool w
    {
        get;
        set;
    }

    public bool a
    {
        get;
        set;
    }

    public bool s
    {
        get;
        set;
    }

    public bool d
    {
        get;
        set;
    }

    public bool space
    {
        get;
        set;
    }

    public bool leftClick
    {
        get;
        set;
    }
}