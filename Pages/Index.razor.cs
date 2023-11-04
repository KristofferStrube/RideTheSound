using KristofferStrube.Blazor.DOM;
using KristofferStrube.Blazor.DOM.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RideTheSound.Events;
using RideTheSound.Extensions;
using System.Drawing;

namespace RideTheSound.Pages;

public partial class Index : ComponentBase, IAsyncDisposable
{
    private EventListener<KeyboardEvent>? keyboardDown;
    private EventListener<KeyboardEvent>? keyboardUp;
    private EventListener<PointerEvent>? mouseDown;
    private EventListener<PointerEvent>? mouseUp;
    private EventListener<PointerEvent>? touchStart;
    private EventListener<PointerEvent>? touchEnd;
    private double windowWidth;
    private double[] curve = [];
    private string curveData = "";
    private DateTimeOffset startTime;
    private DateTimeOffset currentTime;
    private DateTimeOffset endTime;
    private double playerX;
    private double min;
    private double height;
    private CancellationTokenSource? run;
    private (double X, double Y) playerDraw = (0, 0);
    private double playerRadius = 5;
    private double playerHeight = 0;
    private string playerColor = "white";
    private double lineWidth = 1;
    private double energy;
    private BallSize ballSize = BallSize.Big;
    private JumpState ballJump = JumpState.Grounded;
    private double jumpTime = 0;
    private readonly List<SineCurve> sines = [
        new SineCurve(16, Math.PI / 12, 0, 50),
        new SineCurve(12, Math.PI / 3, 100, 50),
        new SineCurve(8, Math.PI / 5, 200, 50),
        new SineCurve(12, Math.PI / 6, 300, 50),
        new SineCurve(7, Math.PI / 8, 400, 50),
        new SineCurve(4, Math.PI / 4, 500, 50),
        new SineCurve(4, Math.PI / 3, 600, 50),
    ];
    private bool dead = false;
    private int score = 0;
    private string token = "";
    private Play? lastPlay;
    private Placement? placement;
    private string playerName = "KSG";

    [Inject]
    public required IJSRuntime JSRuntime { get; set; }

    [Inject]
    public required Sound Sound { get; set; }

    [Inject]
    public required ScoreManager ScoreManager { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await Sound.Init();

        IJSObjectReference windowReference = await JSRuntime.InvokeAsync<IJSObjectReference>("window.valueOf");
        EventTarget windowTarget = EventTarget.CreateAsync(JSRuntime, windowReference);

        var helper = await JSRuntime.GetHelperAsync();
        windowWidth = await helper.InvokeAsync<double>("getAttribute", windowReference, "innerWidth");

        keyboardDown = await EventListener<KeyboardEvent>.CreateAsync(JSRuntime, async (e) =>
        {
            var key = await e.GetKeyAsync();
            if (key is "ArrowRight" or "d")
                await Shrink();
            if (key is "ArrowUp" or "w")
                Jump();
        });
        keyboardUp = await EventListener<KeyboardEvent>.CreateAsync(JSRuntime, async (e) =>
        {
            var key = await e.GetKeyAsync();
            if (key is "ArrowRight" or "d")
                await Grow();
            if (key is "r")
            {
                await Start();
            }
        });
        mouseDown = await EventListener<PointerEvent>.CreateAsync(JSRuntime, async (e) =>
        {
            var buttons = await e.GetButtonsAsync();
            if (buttons is 1 or 3)
                await Shrink();
            if (buttons is 2 or 3)
                Jump();
        });
        mouseUp = await EventListener<PointerEvent>.CreateAsync(JSRuntime, async (e) =>
        {
            var buttons = await e.GetButtonsAsync();
            if (buttons is 0 or 2)
                await Grow();
        });

        touchStart = await EventListener<PointerEvent>.CreateAsync(JSRuntime, async (e) =>
        {
            var touches = await e.GetChangedTouchesAsync();
            var touchCount = await touches.GetLengthAsync();
            if (touchCount > 0)
            {
                var touch = await touches.ItemAsync(0);
                var screenX = await touch.GetClientX();
                if (screenX > windowWidth / 2)
                {
                    await Shrink();
                }
                else
                {
                    Jump();
                }
            }
        });
        touchEnd = await EventListener<PointerEvent>.CreateAsync(JSRuntime, async (e) =>
        {
            await Grow();
        });

        async Task Shrink()
        {
            if (ballSize is BallSize.Big or BallSize.Growing)
            {
                ballSize = BallSize.Shrinking;
                await Sound.PlayDeflate();
            }
        }

        async Task Grow()
        {
            if (ballSize is BallSize.Small or BallSize.Shrinking)
            {
                ballSize = BallSize.Growing;
                await Sound.PlayBlow();
            }
        }

        void Jump()
        {
            if (ballJump is JumpState.Grounded)
            {
                ballJump = JumpState.Ascending;
                jumpTime = 0;
                energy -= 0.5 * Math.Sign(energy);
            }
        }

        await windowTarget.AddEventListenerAsync("keydown", keyboardDown);
        await windowTarget.AddEventListenerAsync("keyup", keyboardUp);
        await windowTarget.AddEventListenerAsync("mousedown", mouseDown);
        await windowTarget.AddEventListenerAsync("mouseup", mouseUp);
        await windowTarget.AddEventListenerAsync("touchstart", touchStart);
        await windowTarget.AddEventListenerAsync("touchend", touchEnd);

        await Start();
    }

    private async Task Start()
    {
        token = await ScoreManager.GetToken();
        lastPlay = null;
        placement = null;
        startTime = DateTimeOffset.UtcNow;
        currentTime = DateTimeOffset.UtcNow;
        playerX = 0;
        energy = 2;

        dead = false;
        score = 0;
        run?.Cancel();
        run = new();
        var cancel = run.Token;

        while (!cancel.IsCancellationRequested)
        {
            await Task.Delay(10);
            MakeCurve();
            StateHasChanged();
        }
    }

    private async Task SubmitScore()
    {
        if (lastPlay is null && placement is null)
        {
            lastPlay = new Play()
            {
                Score = score,
                User = playerName,
                StartTime = startTime,
                EndTime = endTime,
                Token = token
            };
            placement = await ScoreManager.SubmitScore(lastPlay);
        }
    }

    private void MakeCurve()
    {
        double deltaTime = DateTimeOffset.UtcNow.Subtract(currentTime).TotalMilliseconds;
        currentTime = DateTimeOffset.UtcNow;

        var deltaX = energy * deltaTime / playerRadius;
        playerX += deltaX;

        switch (ballSize)
        {
            case BallSize.Growing:
                playerRadius += deltaTime / 100;
                if (playerRadius > 5)
                {
                    playerRadius = 5;
                    ballSize = BallSize.Big;
                }

                break;
            case BallSize.Shrinking:
                playerRadius -= deltaTime / 100;
                if (playerRadius < 3)
                {
                    playerRadius = 3;
                    ballSize = BallSize.Small;
                }
                break;
        }

        switch (ballJump)
        {
            case JumpState.Ascending:
                jumpTime += deltaTime / 100;
                playerHeight = 6 * jumpTime - Math.Pow(jumpTime, 2);
                if (playerHeight < 0)
                {
                    playerHeight = 0;
                    ballJump = JumpState.Grounded;
                }
                break;
        }

        if (dead)
        {
            playerColor = "#404040";
        }
        else
        {
            playerColor = $"#{(int)((playerRadius - 3) / 2 * 160):X2}30{(int)((1 - (playerRadius - 3) / 2) * 160):X2}";
        }

        curve = Enumerable
            .Range(0, 200)
            .Select(i => playerX / 100 + i / 10.0)
            .Select(x => sines.Sum(s => s.Get(x)))
            .ToArray();
        curveData = $"M 0 {curve[0].AsString()} {string.Join(" ", curve[1..].Select((v, i) => $"L {i + 1} {v.AsString()}"))}";
        min = curve.Min() - 2;
        height = curve.Max() - min + 4;
        double angle = Math.Atan(curve[19] - curve[20]);
        double crossingAngle = angle + Math.PI / 2;
        double distanceToMidOfPlayer = playerRadius + playerHeight + lineWidth / 2;
        playerDraw = (20 + Math.Cos(crossingAngle) * distanceToMidOfPlayer, curve[20] - Math.Sin(crossingAngle) * distanceToMidOfPlayer);

        energy += -angle * playerRadius / 100;
        energy *= 1 - Math.Min(0.1, (dead ? 0.01 : deltaTime * 0.000005 * energy * Math.Log2(Math.Abs(playerX * energy))));
        if (!dead && energy < 0)
        {
            dead = true;
            score = (int)(playerX / 100);
            endTime = DateTimeOffset.UtcNow;
        }
    }

    //private double mod(double x, double y) => ((x % y) + y) % y; 

    public ValueTask DisposeAsync()
    {
        run?.Cancel();
        return ValueTask.CompletedTask;
    }

    public enum BallSize
    {
        Big,
        Small,
        Growing,
        Shrinking
    }

    public enum JumpState
    {
        Grounded,
        Ascending,
        Descending,
    }

    public class SineCurve(double amplitude, double frequency, double offset, double warmup)
    {

        public double Get(double x)
        {
            if (x < offset)
            {
                return 0;
            }

            double localX = x - offset;
            var factor = Math.Clamp(localX / warmup, 0, 1);

            return Math.Sin(x * frequency) * amplitude * factor;
        }
    }
}