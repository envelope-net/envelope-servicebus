using Envelope.ServiceBus.Hosts;
using Envelope.Trace;

namespace Envelope.ServiceBus.Internals;

internal static class PublisherHelper
{
	public static string GetPublisherIdentifier(IHostInfo hostInfo, ITraceInfo traceInfo)
	{
		if (hostInfo == null)
			throw new ArgumentNullException(nameof(hostInfo));
		if (traceInfo == null)
			throw new ArgumentNullException(nameof(traceInfo));

		return $"{hostInfo.HostName}:{string.Join($"{Environment.NewLine}> ", traceInfo.TraceFrame.GetTraceMethodIdentifiers())}";
	}
}
