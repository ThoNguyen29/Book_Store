using Book_Store.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json.Serialization;

namespace Book_Store.Components.Cart;

public partial class CartRealtime : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public List<CartItem>? InitialItems { get; set; }

    private readonly Dictionary<int, bool> _updating = new();
    private List<CartItem> _items = new();
    private bool _initialized;

    private decimal Total => _items.Sum(x => x.Price * x.Quantity);

    protected override void OnParametersSet()
    {
        if (_initialized)
        {
            return;
        }

        _items = InitialItems?
            .Select(x => new CartItem
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Price = x.Price,
                Quantity = x.Quantity,
                Image = x.Image
            })
            .ToList() ?? new List<CartItem>();

        _initialized = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await JS.InvokeVoidAsync("cartApi.markConnected");
    }

    private bool IsUpdating(int id) => _updating.TryGetValue(id, out var running) && running;

    private async Task Increase(int id)
    {
        var item = _items.FirstOrDefault(x => x.ProductId == id);
        if (item == null || IsUpdating(id))
        {
            return;
        }

        await SetQuantity(id, item.Quantity + 1);
    }

    private async Task Decrease(int id)
    {
        var item = _items.FirstOrDefault(x => x.ProductId == id);
        if (item == null || IsUpdating(id))
        {
            return;
        }

        await SetQuantity(id, Math.Max(1, item.Quantity - 1));
    }

    private async Task HandleQuantityInput(ChangeEventArgs e, int id)
    {
        if (IsUpdating(id))
        {
            return;
        }

        if (!int.TryParse(e.Value?.ToString(), out var qty) || qty < 1)
        {
            qty = 1;
        }

        await SetQuantity(id, qty);
    }

    private async Task SetQuantity(int id, int quantity)
    {
        var item = _items.FirstOrDefault(x => x.ProductId == id);
        if (item == null)
        {
            return;
        }

        var oldQuantity = item.Quantity;
        item.Quantity = quantity;
        _updating[id] = true;

        try
        {
            var response = await JS.InvokeAsync<CartSyncResponse>("cartApi.setQuantity", id, quantity);
            if (response?.Success == true)
            {
                if (response.Removed)
                {
                    _items.RemoveAll(x => x.ProductId == id);
                }
                else
                {
                    item.Quantity = response.Quantity > 0 ? response.Quantity : quantity;
                }

                await SyncMiniCart(response);
            }
            else
            {
                item.Quantity = oldQuantity;
            }
        }
        catch
        {
            item.Quantity = oldQuantity;
        }
        finally
        {
            _updating[id] = false;
        }
    }

    private async Task Remove(int id)
    {
        var removed = _items.FirstOrDefault(x => x.ProductId == id);
        if (removed == null || IsUpdating(id))
        {
            return;
        }

        _updating[id] = true;
        _items.Remove(removed);

        try
        {
            var response = await JS.InvokeAsync<CartSyncResponse>("cartApi.removeItem", id);
            if (response?.Success == true)
            {
                await SyncMiniCart(response);
            }
            else
            {
                _items.Add(removed);
                _items = _items.OrderBy(x => x.ProductId).ToList();
            }
        }
        catch
        {
            _items.Add(removed);
            _items = _items.OrderBy(x => x.ProductId).ToList();
        }
        finally
        {
            _updating[id] = false;
        }
    }

    private async Task SyncMiniCart(CartSyncResponse response)
    {
        await JS.InvokeVoidAsync("cartApi.syncMiniCart", response.Count, response.Total);
    }

    private sealed class CartSyncResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("removed")]
        public bool Removed { get; set; }
    }
}
