using KristofferStrube.Blazor.DOM;
using KristofferStrube.Blazor.DOM.Extensions;
using Microsoft.JSInterop;

namespace RideTheSound.Events;

public class Touch
{
    protected readonly Lazy<Task<IJSObjectReference>> helperTask;
    public IJSObjectReference JSReference { get; }
    public IJSRuntime JSRuntime { get; }

    public Touch(IJSRuntime jSRuntime, IJSObjectReference jSReference)
    {
        helperTask = new Lazy<Task<IJSObjectReference>>(jSRuntime.GetHelperAsync);
        JSReference = jSReference;
        JSRuntime = jSRuntime;
    }

    public async Task<double> GetClientX()
    {
        var helper = await helperTask.Value;
        return await helper.InvokeAsync<double>("getAttribute", JSReference, "clientX");
    }

    public async Task<double> GetClientY()
    {
        var helper = await helperTask.Value;
        return await helper.InvokeAsync<double>("getAttribute", JSReference, "clientY");
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
