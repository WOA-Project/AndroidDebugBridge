using System;
using System.Linq;

namespace AndroidDebugBridge
{
    public partial class AndroidDebugBridgeTransport
    {
        public (string variableName, string variableValue)[]? GetAllVariables()
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot get all variables with no accepted connection!");
            }

            (string variableName, string variableValue)[]? result = null;

            try
            {
                string getPropAnswer = Shell("getprop");

                return getPropAnswer.Split("\n")
                                    .Where(t => 
                                                t.Contains(':') && 
                                                t.Contains('[') && 
                                                t.Contains(']') && 
                                                t.Contains("]: ["))
                                    .Select(t =>
                                    {
                                        string[] cleanedUpPart = t.Trim()[1..^1].Split("]: [");
                                        return (cleanedUpPart[0], string.Join("]: [", cleanedUpPart.Skip(1)));
                                    })
                                    .ToArray();
            }
            catch (Exception) { }

            return result;
        }

        public string? GetVariableValue(string variableName)
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot get a specific variable with no accepted connection!");
            }

            string? result = null;

            try
            {
                result = Shell($"getprop {variableName}");
            }
            catch (Exception) { }

            return result;
        }
    }
}