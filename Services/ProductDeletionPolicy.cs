namespace WebServiceCosmetics.Services;

/// <summary>
/// Applies the product deletion rule without depending on MVC or database details.
/// </summary>
public static class ProductDeletionPolicy
{
    public static bool HasHistory(bool hasManufacturing, bool hasSales)
    {
        return hasManufacturing || hasSales;
    }
}