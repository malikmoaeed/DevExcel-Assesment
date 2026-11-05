namespace InventoryManagement.Application.Exceptions;

public sealed class NotFoundException() : Exception("The requested product was not found.");
