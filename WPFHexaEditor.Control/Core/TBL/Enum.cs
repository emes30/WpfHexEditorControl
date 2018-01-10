namespace WPFHexaEditor.Core.TBL
{
    /// <summary>
    /// Type de DTE qui sera utilisé dans les classe de DTE
    /// 
    /// Derek Tremblay 2003-2017
    /// </summary>
    public enum DTEType
    {
        Invalid = -1,
        ASCII = 0,
        Japonais,
        DualTitleEncoding,
        MultipleTitleEncoding,
        EndLine,
        EndBlock
    }
}