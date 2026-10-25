using System;
using System.Runtime.InteropServices;

namespace ThemeExplorer.Core
{
    /// <summary>
    /// Thrown when a UXTHEME.DLL function reports a failure HRESULT.
    /// <para xml:lang="zh-CN">当 UXTHEME.DLL 函数返回失败的 HRESULT 时抛出。</para>
    /// </summary>
    [Serializable]
    public class ThemeException : Exception
    {
        /// <summary>
        /// Creates an exception for the given operation and HRESULT.
        /// <para xml:lang="zh-CN">为指定操作和 HRESULT 创建异常。</para>
        /// </summary>
        public ThemeException(string operation, int nativeHResult)
            : base(BuildMessage(operation, nativeHResult))
        {
            Operation = operation;
            NativeHResult = nativeHResult;
            HResult = nativeHResult;
        }

        /// <summary>
        /// Creates an exception for the given operation, HRESULT and inner exception.
        /// <para xml:lang="zh-CN">为指定操作、HRESULT 和内部异常创建异常。</para>
        /// </summary>
        public ThemeException(string operation, int nativeHResult, Exception innerException)
            : base(BuildMessage(operation, nativeHResult), innerException)
        {
            Operation = operation;
            NativeHResult = nativeHResult;
            HResult = nativeHResult;
        }

        /// <summary>
        /// Name of the native API that failed.
        /// <para xml:lang="zh-CN">失败的本机 API 名称。</para>
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// The HRESULT returned by the native API.
        /// <para xml:lang="zh-CN">本机 API 返回的 HRESULT。</para>
        /// </summary>
        public int NativeHResult { get; }

        /// <summary>
        /// The COM exception corresponding to <see cref="NativeHResult"/>, if any.
        /// <para xml:lang="zh-CN">与 <see cref="NativeHResult"/> 对应的 COM 异常（如果有）。</para>
        /// </summary>
        public COMException AsComException()
        {
            return new COMException(Message, NativeHResult);
        }

        private static string BuildMessage(string operation, int nativeHResult)
        {
            string name = operation ?? "UXTHEME";
            return string.Format("The visual style operation '{0}' failed (HRESULT 0x{1:X8}).", name, nativeHResult);
        }
    }
}
