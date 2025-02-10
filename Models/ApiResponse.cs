using Newtonsoft.Json;
using System;  // 添加这个命名空间

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Msg { get; set; }
    public T Data { get; set; }

    // 默认构造函数
    public ApiResponse() { }

    // 带参数构造函数
    public ApiResponse(int code, string msg)
    {
        Code = code;
        Msg = msg;
    }

    // 带参数构造函数
    public ApiResponse(int code, string msg, T data)
    {
        Code = code;
        Msg = msg;
        Data = data;
    }

    // 使用预设方法创建 ApiResponse
    public static ApiResponse<T> Success()
    {
        return new ApiResponse<T>(0, "Success");
    }

    public static ApiResponse<T> Success(T data)
    {
        return new ApiResponse<T>(0, "Success", data);
    }

    // 序列化方法：将 ApiResponse 对象转换为 JSON 字符串
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }

    // 反序列化方法：将 JSON 字符串转换为 ApiResponse 对象
    public static ApiResponse<T> FromJson(string json)
    {
        return JsonConvert.DeserializeObject<ApiResponse<T>>(json);
    }
}
