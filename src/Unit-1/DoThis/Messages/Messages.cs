namespace WinTail
{
    public class Messages
    {
       #region System Messages
       
       public class ContinueProcessing {}
       
       #endregion

       #region Success Messages
       
       public class InputSuccess
       {
           public string Reason { get; }

           public InputSuccess(string reason)
           {
               Reason = reason;
           }
       }
       
       #endregion
       
       #region Failure Messages

       public class InputError
       {
           public string Reason { get; }

           public InputError(string reason)
           {
               Reason = reason;
           }
       }

       public class NullInputError : InputError
       {
           public NullInputError(string reason) : base(reason) {}
       }

       public class ValidationError : InputError
       {
           public ValidationError(string reason) : base(reason) {}
       }
       
       #endregion
    }
}