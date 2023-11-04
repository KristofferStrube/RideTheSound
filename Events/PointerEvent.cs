using KristofferStrube.Blazor.DOM;
using KristofferStrube.Blazor.WebIDL;
using Microsoft.JSInterop;

namespace RideTheSound.Events;

public class PointerEvent : Event, IJSCreatable<PointerEvent>
{
    public static new Task<PointerEvent> CreateAsync(IJSRuntime jSRuntime, IJSObjectReference jSReference)
    {
        return Task.FromResult<PointerEvent>(new(jSRuntime, jSReference));
    }

    protected PointerEvent(IJSRuntime jSRuntime, IJSObjectReference jSReference) : base(jSRuntime, jSReference)
    {
    }

    public async Task<int> GetButtonsAsync()
    {
        var helper = await helperTask.Value;
        return await helper.InvokeAsync<int>("getAttribute", JSReference, "buttons");
    }

    public async Task<TouchList> GetTouchesAsync()
    {
        var helper = await helperTask.Value;
        var jSInstance = await helper.InvokeAsync<IJSObjectReference>("getAttribute", JSReference, "touches");
        return new TouchList(JSRuntime, jSInstance);
    }

    public async Task<TouchList> GetChangedTouchesAsync()
    {
        var helper = await helperTask.Value;
        var jSInstance = await helper.InvokeAsync<IJSObjectReference>("getAttribute", JSReference, "changedTouches");
        return new TouchList(JSRuntime, jSInstance);
    }
}
