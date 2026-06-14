namespace BuildingBlocks.Aspects;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipExecutionLoggingAttribute : Attribute;
