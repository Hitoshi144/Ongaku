using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting.Server;
using MudBlazor;
using MudBlazor.Services;
using System;

namespace Ongaku.Services {
    public class ViewportService {
        private readonly IBrowserViewportService _browserViewportService;

        public BrowserWindowSize CurrentSize { get; private set; } = new();
        public event Action<BrowserWindowSize>? OnResize;

        public ViewportService(IBrowserViewportService browserViewportService)
        {
            _browserViewportService = browserViewportService;
        }

        public async Task InitAsync()
        {
            await _browserViewportService.SubscribeAsync(observer: new Observer(this), fireImmediately: true);
        }

        private void UpdateSize(BrowserWindowSize size)
        {
            CurrentSize = size;
            OnResize?.Invoke(size);
        }

        private class Observer : IBrowserViewportObserver {
            private readonly ViewportService _service;
            public Guid Id { get; } = Guid.NewGuid();

            public Observer(ViewportService service)
            {
                _service = service;
            }

            public Task NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs args)
            {
                _service.UpdateSize(args.BrowserWindowSize);
                return Task.CompletedTask;
            }
        }
    }
}
