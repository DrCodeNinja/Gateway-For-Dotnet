var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Sample in-memory catalog data
var products = new List<Product>
{
    new(1, "Laptop", "High-performance laptop", 1299.99m, 50),
    new(2, "Mouse", "Wireless ergonomic mouse", 29.99m, 200),
    new(3, "Keyboard", "Mechanical keyboard", 89.99m, 150),
    new(4, "Monitor", "27-inch 4K display", 499.99m, 75),
    new(5, "Headset", "Noise-cancelling headset", 149.99m, 120)
};

// GET /api/products - list all products
app.MapGet("/api/products", () =>
{
    app.Logger.LogInformation("Catalog: Returning {Count} products", products.Count);
    return Results.Ok(products);
});

// GET /api/products/{id} - get single product
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    if (product is null)
        return Results.NotFound(new { message = $"Product {id} not found" });

    app.Logger.LogInformation("Catalog: Returning product {Id}", id);
    return Results.Ok(product);
});

// POST /api/products - create product
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    var newId = products.Max(p => p.Id) + 1;
    var product = new Product(newId, request.Name, request.Description, request.Price, request.Stock);
    products.Add(product);

    app.Logger.LogInformation("Catalog: Created product {Id} - {Name}", newId, request.Name);
    return Results.Created($"/api/products/{newId}", product);
});

// Health check
app.MapHealthChecks("/health");

app.Run();

record Product(int Id, string Name, string Description, decimal Price, int Stock);
record CreateProductRequest(string Name, string Description, decimal Price, int Stock);
