using KristofferStrube.Blazor.WebAudio;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RideTheSound;

public class Sound
{
    private AudioContext context = default!;
    private AudioDestinationNode destination = default!;

    private AudioBuffer deflateAudioBuffer = default!;
    private AudioBuffer blowAudioBuffer = default!;

    public required HttpClient HttpClient { get; set; }

    public required IJSRuntime JSRuntime { get; set; }

    public Sound(HttpClient httpClient, IJSRuntime jSRuntime)
    {
        HttpClient = httpClient;
        JSRuntime = jSRuntime;
    }

    public async Task Init()
    {
        context = await AudioContext.CreateAsync(JSRuntime);
        destination = await context.GetDestinationAsync();

        byte[] deflateData = await HttpClient.GetByteArrayAsync("sound/deflate.mp3");
        deflateAudioBuffer = await context.DecodeAudioDataAsync(deflateData);

        byte[] blowData = await HttpClient.GetByteArrayAsync("sound/blow.mp3");
        blowAudioBuffer = await context.DecodeAudioDataAsync(blowData);
    }

    public async Task PlayBlow()
    {
        AudioBufferSourceNode blowNode = await AudioBufferSourceNode.CreateAsync(JSRuntime, context, new AudioBufferSourceOptions()
        {
            PlaybackRate = 2,
            Buffer = blowAudioBuffer
        });
        GainNode gainNode = await context.CreateGainAsync();

        await blowNode.ConnectAsync(gainNode);
        await gainNode.ConnectAsync(destination);
        await blowNode.StartAsync();

        var gainParam = await gainNode.GetGainAsync();
        var time = await context.GetCurrentTimeAsync();
        await gainParam.LinearRampToValueAtTimeAsync(0, time + 0.2);
    }

    public async Task PlayDeflate()
    {
        AudioBufferSourceNode deflateNode = await AudioBufferSourceNode.CreateAsync(JSRuntime, context, new AudioBufferSourceOptions()
        {
            PlaybackRate = 2,
            Buffer = deflateAudioBuffer
        });
        GainNode gainNode = await context.CreateGainAsync();

        await deflateNode.ConnectAsync(gainNode);
        await gainNode.ConnectAsync(destination);
        await deflateNode.StartAsync();

        var gainParam = await gainNode.GetGainAsync();
        var time = await context.GetCurrentTimeAsync();
        await gainParam.LinearRampToValueAtTimeAsync(1, time + 0.1);
        await gainParam.LinearRampToValueAtTimeAsync(0, time + 0.4);
    }
}
