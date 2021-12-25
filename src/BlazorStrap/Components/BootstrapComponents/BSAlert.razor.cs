﻿using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;

namespace BlazorStrap
{
    public partial class BSAlert : BlazorStrapBase
    {
        [Parameter] public BSColor Color { get; set; } = BSColor.Default;
        [Parameter] public RenderFragment? Content { get; set; }
        [Parameter] public EventCallback Dismissed { get; set; }
        [Parameter] public bool HasIcon { get; set; }
        [Parameter] public RenderFragment? Header { get; set; }
        [Parameter] public int Heading { get; set; } = 1;
        [Parameter] public bool IsDismissable { get; set; }

        private bool _dismissed { get; set; }

        private string? ClassBuilder => new CssBuilder("alert")
            .AddClass($"alert-{BSColor.GetName<BSColor>(Color).ToLower()}", Color != BSColor.Default)
            .AddClass("d-flex align-items-center", HasIcon)
            .AddClass("alert-dismissible", IsDismissable)
            .AddClass(LayoutClass, !string.IsNullOrEmpty(LayoutClass))
            .AddClass(Class, !string.IsNullOrEmpty(Class))
            .Build().ToNullString();

        public async Task CloseEventAsync()
        {
            await Dismissed.InvokeAsync();
            _dismissed = true;
            await InvokeAsync(StateHasChanged);
        }

        public void Open()
        {
            _dismissed = false;
        }
    }
}