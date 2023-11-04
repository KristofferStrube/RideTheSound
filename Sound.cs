using KristofferStrube.Blazor.WebAudio;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RideTheSound;

public class Sound
{
    private readonly HttpClient httpClient;
    private readonly IJSRuntime jSRuntime;

    private AudioContext context = default!;
    private AudioDestinationNode destination = default!;

    private bool isPlaying = false;

    public float BPM { get; set; } = 160;

    private static string[] files = [
        "balloon_slap",
        "blow",
        "deflate",
        "nut",
        "nut_in_balloon",
        "tin_tap"
    ];

    private Dictionary<string, AudioBuffer> audioBuffers = new();

    public Sound(HttpClient httpClient, IJSRuntime jSRuntime)
    {
        this.httpClient = httpClient;
        this.jSRuntime = jSRuntime;
    }

    public async Task Init()
    {
        context = await AudioContext.CreateAsync(jSRuntime);
        destination = await context.GetDestinationAsync();

        foreach (string file in files)
        {
            byte[] data = await httpClient.GetByteArrayAsync($"sound/{file}.mp3");
            audioBuffers.Add(file, await context.DecodeAudioDataAsync(data));
        }
    }

    public async Task PlayMusic()
    {
        if (isPlaying) return;
        isPlaying = true;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(async () =>
        {
            int round = 0;

            int[] bases = [0, 1, 0, 7, 2, 0, 2, 2, 6, 7, 8, 7, 0, 4, 2, 7, 0, 1, 0, 6, 2, 0, 4, 4, 7, 8, 9, 7, 0, 4, 2, 7];

            // C  = 0
            // C# = 1
            // D  = 2
            // D# = 3
            // E  = 4
            // F  = 5
            // F# = 6
            // G  = 7
            // G# = 8
            // A  = 9
            // A# = 10
            // B  = 11

            while (true)
            {
                var time = await context.GetCurrentTimeAsync();

                await PlaySound("balloon_slap", 4, 1);

                await PlayKey(5, bases[round % bases.Length], 0.1f, 0.5f);

                if (round % 4 is 0 or 2)
                {
                    await PlaySound("nut", 0.3f, 1f, 0);
                }

                if (round % 4 is 0)
                {
                    await PlaySound("nut_in_balloon", 2, 0.5f);
                }

                while (await context.GetCurrentTimeAsync() < time + 60 / BPM)
                {
                    await Task.Delay(20);
                }
                round++;
            }
        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    public async Task PlayBlow()
    {
        await using GainNode gainNode = await PlaySound("blow", 0, 2);

        var gainParam = await gainNode.GetGainAsync();
        var time = await context.GetCurrentTimeAsync();
        await gainParam.LinearRampToValueAtTimeAsync(0.5f, time + 0.05);
        await gainParam.LinearRampToValueAtTimeAsync(0, time + 0.35);
        disconnectNodeAndDisposeAfter(gainNode, 0.4f);
    }

    public async Task PlayDeflate()
    {
        GainNode gainNode = await PlaySound("deflate", 0, 2);

        var gainParam = await gainNode.GetGainAsync();
        var time = await context.GetCurrentTimeAsync();
        await gainParam.LinearRampToValueAtTimeAsync(0.5f, time + 0.1);
        await gainParam.LinearRampToValueAtTimeAsync(0, time + 0.4);
        disconnectNodeAndDisposeAfter(gainNode, 0.5f);
    }

    public async Task<GainNode> PlaySound(string file, float gain, float speed, double time = 0)
    {
        await using AudioBufferSourceNode audioNode = await AudioBufferSourceNode.CreateAsync(jSRuntime, context, new AudioBufferSourceOptions()
        {
            PlaybackRate = speed,
            Buffer = audioBuffers[file]
        });
        GainNode gainNode = await GainNode.CreateAsync(jSRuntime, context, new() { Gain = gain });

        await audioNode.ConnectAsync(gainNode);
        await gainNode.ConnectAsync(destination);
        await audioNode.StartAsync(time);

        disconnectNodeAndDisposeAfter(audioNode, 1);

        return gainNode;
    }

    public async Task PlayKey(int octave, int pitch, float gain, float length)
    {
        OscillatorNode oscillator = await OscillatorNode.CreateAsync(jSRuntime, context, new()
        {
            Type = OscillatorType.Sine,
            Frequency = (float)Frequency(octave, pitch)
        });
        GainNode gainNode = await context.CreateGainAsync();

        await oscillator.ConnectAsync(gainNode);
        await gainNode.ConnectAsync(destination);
        await oscillator.StartAsync();

        await using var gainParam = await gainNode.GetGainAsync();
        var time = await context.GetCurrentTimeAsync();
        await gainParam.LinearRampToValueAtTimeAsync(gain, time + length * 1 / 4);
        await gainParam.LinearRampToValueAtTimeAsync(0, time + length * 3 / 4);

        disconnectNodeAndDisposeAfter(oscillator, length);
        disconnectNodeAndDisposeAfter(gainNode, length);
    }

    private double Frequency(int octave, int pitch)
    {
        var noteIndex = octave * 12 + pitch;
        var a = Math.Pow(2, 1.0 / 12);
        var A4 = 440;
        var A4Index = 4 * 12 + 10;
        var halfStepDifference = noteIndex - A4Index;
        return A4 * Math.Pow(a, halfStepDifference);
    }

    private void disconnectNodeAndDisposeAfter(AudioNode node, float time)
    {
        Task.Run(async () =>
        {
            await Task.Delay((int)(time * 1000));
            await node.DisconnectAsync();
            await node.DisposeAsync();
        });
    }
}
