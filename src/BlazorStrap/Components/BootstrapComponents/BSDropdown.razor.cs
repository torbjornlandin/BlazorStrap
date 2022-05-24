using BlazorComponentUtilities;
using BlazorStrap.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorStrap
{
    public partial class BSDropdown : BlazorStrapBase, IAsyncDisposable
    {
        private Func<Task>? _callback;

        /// <summary>
        /// Clicking inside the dropdown menu will not close it.
        /// </summary>
        [Parameter] public bool AllowItemClick { get; set; }

        /// <summary>
        /// Clicks outside of the dropdown will not cause the dropdown to close.
        /// </summary>
        [Parameter] public bool AllowOutsideClick { get; set; }

        /// <summary>
        /// Dropdown menu content.
        /// </summary>
        [Parameter] public RenderFragment? Content { get; set; }

        /// <summary>
        /// Hides the dropdown button and only shows the content.
        /// </summary>
        [Parameter] public bool Demo { get; set; }

        /// <summary>
        /// Adds the <c>dropdown-menu-dark</c> css class making the dropdown content dark.
        /// </summary>
        [Parameter] public bool IsDark { get; set; }

        /// <summary>
        /// Renders the dropdown menu with a <c>div</c> and uses popper.js to create.
        /// </summary>
        [Parameter] public bool IsDiv { get; set; }

        /// <summary>
        /// A combination of <see cref="AllowItemClick"/> and <see cref="AllowOutsideClick"/>.
        /// Requires the dropdown to be closed by clicking the button again.
        /// </summary>
        [Parameter] public bool IsManual { get; set; }

        /// <summary>
        /// Renders dropdown as a <see cref="BSPopover"/> element and sets <see cref="BSPopover.IsNavItemList"/> true.
        /// </summary>
        [Parameter] public bool IsNavPopper { get; set; }

        /// <summary>
        /// Disables dynamic positioning.
        /// </summary>
        [Parameter] public bool IsStatic { get; set; }

        /// <summary>
        /// Dropdown offset.
        /// </summary>
        [Parameter] public string? Offset { get; set; }

        /// <summary>
        /// Dropdown placement.
        /// </summary>
        [Parameter] public Placement Placement { get; set; } = Placement.RightStart;

        /// <summary>
        /// Attribute to add when dropdown is shown.
        /// </summary>
        [Parameter] public string? ShownAttribute { get; set; }

        /// <summary>
        /// data-blazorstrap data Id of target element
        /// </summary>
        [Parameter] public string Target { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Element to be used to toggle the dropdown.
        /// </summary>
        [Parameter] public RenderFragment? Toggler { get; set; }

        private bool _lastIsNavPopper;
        private DotNetObjectReference<BSDropdown>? _objectRef;
        [CascadingParameter] public BSNavItem? DropdownItem { get; set; }
        [CascadingParameter] public BSButtonGroup? Group { get; set; }
        [CascadingParameter] public BSNavItem? NavItem { get; set; }
        [CascadingParameter] public BSDropdown? Parent { get; set; }
        internal bool Active { get; private set; }
        internal int ChildCount { get; set; }

        private string? ClassBuilder => new CssBuilder("dropdown-menu")
            .AddClass("dropdown-menu-dark", IsDark)
            .AddClass("show", Shown)
            .AddClass(LayoutClass, !string.IsNullOrEmpty(LayoutClass))
            .AddClass(Class, !string.IsNullOrEmpty(Class))
            .Build().ToNullString();

        private string DataRefId => (PopoverRef != null) ? PopoverRef.DataId : DataId;

        private string? GroupClassBuilder => new CssBuilder()
            .AddClass(LayoutClass, !string.IsNullOrEmpty(LayoutClass))
            .AddClass(Class, !string.IsNullOrEmpty(Class))
            .Build().ToNullString();

        private string? IsDivClassBuilder => new CssBuilder()
            .AddClass("dropdown", Parent == null)
            .AddClass("dropup", Placement is Placement.Top or Placement.TopEnd or Placement.TopStart)
            .AddClass("dropstart", Placement is Placement.Left or Placement.LeftEnd or Placement.LeftStart)
            .AddClass("dropend", Placement is Placement.Right or Placement.RightEnd or Placement.RightStart)
            .Build().ToNullString();

        private ElementReference? MyRef { get; set; }
        internal Action<bool, BSDropdownItem>? OnSetActive { get; set; }
        private BSPopover? PopoverRef { get; set; }

        /// <summary>
        /// Whether or not the dropdown is shown.
        /// </summary>
        public bool Shown { get; private set; }

        private async Task TryCallback(bool renderOnFail = true)
        {
            try
            {
                // Check if objectRef set if not callback will be handled after render.
                // If anything fails callback will will be handled after render.
                if (_objectRef != null)
                {
                    if (_callback != null)
                    {
                        await _callback();
                        _callback = null;
                    }
                }
                else
                {
                    throw new InvalidOperationException("No object ref");
                }
            }
            catch
            {
                if (renderOnFail)
                    await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// Hide the dropdown
        /// </summary>
        /// <returns>Completed task once hide is complete.</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public Task HideAsync()
        {
            if (!Shown) return Task.CompletedTask;
            _callback = async () =>
            {
                await HideActionsAsync();
            };
            return TryCallback();
        }

        private async Task HideActionsAsync()
        {
            Shown = false;
            await BlazorStrap.Interop.RemoveDocumentEventAsync(this, DataRefId, EventType.Click);

            if ((Group != null && PopoverRef != null && !IsStatic) || (IsDiv || Parent != null || IsNavPopper))
            {
                if (PopoverRef != null)
                    await PopoverRef.HideAsync();
            }

            if (!string.IsNullOrEmpty(ShownAttribute))
            {
                await BlazorStrap.Interop.RemoveAttributeAsync(MyRef, ShownAttribute);
            }

            await InvokeAsync(StateHasChanged);
        }

        public override async Task InteropEventCallback(string id, CallerName name, EventType type)
        {
            if (id == DataId && name.Equals(typeof(ClickForward)) && type == EventType.Click)
            {
                await ToggleAsync();
            }
        }

        [JSInvokable]
        public override async Task InteropEventCallback(string id, CallerName name, EventType type,
            Dictionary<string, string>? classList, JavascriptEvent? e)
        {
            // The if statement was getting hard to read so split into parts 
            if (id == DataRefId && name.Equals(this) && type == EventType.Click)
            {
                // If this dropdown toggle return
                if (e?.Target.ClassList.Any(q => q.Value == "dropdown-toggle") == true &&
                    e.Target.TargetId == DataId) return;

                // If click element is inside this dropdown return
                // if (e?.Target.ChildrenId?.Any(q => q == DataId) == true && AllowItemClick) return;
                // If is Manual Return
                if (IsManual) return;
                await HideAsync();
            }
        }

        /// <summary>
        /// Show the dropdown.
        /// </summary>
        /// <returns>Completed task when the dropdown is shown.</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public Task ShowAsync()
        {
            if (Shown) return Task.CompletedTask;
            _callback = async () =>
            {
                await ShowActionsAsync();
            };
            return TryCallback();
        }

        private async Task ShowActionsAsync()
        {
            Shown = true;

            if (!AllowOutsideClick)
            {
                await BlazorStrap.Interop.AddDocumentEventAsync(_objectRef, DataRefId, EventType.Click, AllowItemClick);
            }

            if ((Group != null && PopoverRef != null && !IsStatic) || (IsDiv || Parent != null || IsNavPopper))
            {
                if (PopoverRef != null) await PopoverRef.ShowAsync();
            }

            if (!string.IsNullOrEmpty(ShownAttribute))
            {
                await BlazorStrap.Interop.AddAttributeAsync(MyRef, ShownAttribute, "blazorStrap");
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Toggles dropdown open or closed.yuo
        /// </summary>
        /// <returns>Completed task once dropdown is open or closed.</returns>
        public Task ToggleAsync()
        {
            return Shown ? HideAsync() : ShowAsync();
        }

        protected override void OnInitialized()
        {
            _lastIsNavPopper = IsNavPopper;
        }

        protected override void OnParametersSet()
        {
            if (IsNavPopper == false)
            {
                if (_lastIsNavPopper != IsNavPopper)
                {
                    PopoverRef = null;
                }
            }
            else
            {
                if (IsNavPopper != _lastIsNavPopper == false)
                {
                    Shown = false;
                    StateHasChanged();
                }
            }

            _lastIsNavPopper = IsNavPopper;
        }

        internal void SetActive(bool active, BSDropdownItem item)
        {
            OnSetActive?.Invoke(active, item);
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _objectRef = DotNetObjectReference.Create<BSDropdown>(this);
                BlazorStrap.OnEventForward += InteropEventCallback;
            }
            if (_callback != null)
            {
                await _callback.Invoke();
                _callback = null;
            }
        }
        public async ValueTask DisposeAsync()
        {
            _objectRef?.Dispose();
            try
            {
                await BlazorStrap.Interop.RemoveDocumentEventAsync(this, DataRefId, EventType.Click);
            }
            catch { }
            BlazorStrap.OnEventForward -= InteropEventCallback;
            GC.SuppressFinalize(this);
        }
    }
}