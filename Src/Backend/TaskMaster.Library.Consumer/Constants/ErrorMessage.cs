using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Consumer.Constants
{
    internal class ErrorMessage
    {
        //internal static string JobTypeAttributeNotFound(string classFullName) => $"Payload type '{classFullName}' must be decorated with JobTypeAttribute.";
        //internal static string JsonSchemaValidationFailed(IEnumerable<string> errors) => $"Payload does not match the job type schema: {string.Join("; ", errors)}";
        internal static string ConsumerServiceAlreadyInitialised() => $"Consumer service already initialised";
        internal static string ConsumerServiceNotInitialised() => $"Consumer services not initilised";

    }
}
