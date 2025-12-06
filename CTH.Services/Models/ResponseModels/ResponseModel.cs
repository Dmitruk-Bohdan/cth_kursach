namespace CTH.Services.Models.ResponseModels;

public class ResponseModelBase
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ResponseModel : ResponseModelBase
{
    public object? Result { get; set; }
}

public class ResponseModel<TModel> : ResponseModelBase
{
    public TModel? Result { get; set; }
}
