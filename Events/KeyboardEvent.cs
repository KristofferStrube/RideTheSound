using KristofferStrube.Blazor.DOM;
using KristofferStrube.Blazor.WebIDL;
using Microsoft.JSInterop;

namespace RideTheSound.Events;

public class KeyboardEvent : Event, IJSCreatable<KeyboardEvent>
{
    public static new Task<KeyboardEvent> CreateAsync(IJSRuntime jSRuntime, IJSObjectReference jSReference)
    {
        return Task.FromResult<KeyboardEvent>(new(jSRuntime, jSReference));
    }

    protected KeyboardEvent(IJSRuntime jSRuntime, IJSObjectReference jSReference) : base(jSRuntime, jSReference)
    {
    }

    public async Task<string> GetKeyAsync()
    {
        var helper = await helperTask.Value;
        return await helper.InvokeAsync<string>("getAttribute", JSReference, "key");
    }
}
