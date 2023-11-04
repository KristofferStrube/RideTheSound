using KristofferStrube.Blazor.DOM;
using KristofferStrube.Blazor.DOM.Extensions;
using Microsoft.JSInterop;

namespace RideTheSound.Events;

public class TouchList
{
    protected readonly Lazy<Task<IJSObjectReference>> helperTask;
    public IJSObjectReference JSReference { get; }
    public IJSRuntime JSRuntime { get; }

    public TouchList(IJSRuntime jSRuntime, IJSObjectReference jSReference)
    {
        helperTask = new Lazy<Task<IJSObjectReference>>(jSRuntime.GetHelperAsync);
        JSReference = jSReference;
        JSRuntime = jSRuntime;
    }

    public async Task<ulong> GetLengthAsync()
    {
        var helper = await helperTask.Value;
        return await helper.InvokeAsync<ulong>("getAttribute", JSReference, "length");
    }

    public async Task<Touch> ItemAsync(ulong i)
    {
        var helper = await helperTask.Value;
        var jSInstance = await helper.InvokeAsync<IJSObjectReference>("getAttribute", JSReference, i);
        return new Touch(JSRuntime, jSInstance);
    }

    public async ValueTask DisposeAsync()
    {
        if (helperTask.IsValueCreated)
        {
            await (await helperTask.Value).DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
