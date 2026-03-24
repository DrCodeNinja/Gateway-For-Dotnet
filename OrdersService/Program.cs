var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Sample in-memory orders data
var orders = new List<Order>
{
    new(1, "CUST-001", [new OrderItem(1, "Laptop", 1, 1299.99m)], OrderStatus.Completed, DateTime.Parse("2026-03-20")),
    new(2, "CUST-002", [new OrderItem(2, "Mouse", 2, 29.99m), new OrderItem(3, "Keyboard", 1, 89.99m)], OrderStatus.Processing, DateTime.Parse("2026-03-23")),
    new(3, "CUST-001", [new OrderItem(4, "Monitor", 1, 499.99m)], OrderStatus.Pending, DateTime.Parse("2026-03-24"))
};

var nextId = 4;

// GET /api/orders - list all orders
app.MapGet("/api/orders", () =>
{
    app.Logger.LogInformation("Orders: Returning {Count} orders", orders.Count);
    return Results.Ok(orders);
});

// GET /api/orders/{id} - get single order
app.MapGet("/api/orders/{id:int}", (int id) =>
{
    var order = orders.FirstOrDefault(o => o.Id == id);
    if (order is null)
        return Results.NotFound(new { message = $"Order {id} not found" });

    app.Logger.LogInformation("Orders: Returning order {Id}", id);
    return Results.Ok(order);
});

// POST /api/orders - create order
app.MapPost("/api/orders", (CreateOrderRequest request) =>
{
    var order = new Order(nextId++, request.CustomerId, request.Items, OrderStatus.Pending, DateTime.UtcNow);
    orders.Add(order);

    app.Logger.LogInformation("Orders: Created order {Id} for customer {CustomerId}", order.Id, request.CustomerId);
    return Results.Created($"/api/orders/{order.Id}", order);
});

// PUT /api/orders/{id}/status - update order status
app.MapPut("/api/orders/{id:int}/status", (int id, UpdateStatusRequest request) =>
{
    var order = orders.FirstOrDefault(o => o.Id == id);
    if (order is null)
        return Results.NotFound(new { message = $"Order {id} not found" });

    var index = orders.IndexOf(order);
    orders[index] = order with { Status = request.Status };

    app.Logger.LogInformation("Orders: Updated order {Id} status to {Status}", id, request.Status);
    return Results.Ok(orders[index]);
});

// Health check
app.MapHealthChecks("/health");

app.Run();

record Order(int Id, string CustomerId, List<OrderItem> Items, OrderStatus Status, DateTime CreatedAt)
{
    public decimal Total => Items.Sum(i => i.Quantity * i.UnitPrice);
}
record OrderItem(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
record CreateOrderRequest(string CustomerId, List<OrderItem> Items);
record UpdateStatusRequest(OrderStatus Status);

enum OrderStatus { Pending, Processing, Completed, Cancelled }
