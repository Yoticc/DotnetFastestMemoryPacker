using PatcherReference;

namespace DotnetFastestMemoryPacker.Internal;

[ShouldBeTrimmed]
struct GCAllocFlags
{
	public const uint NoFlags = 0;
	public const uint ZeroingOptional = 16;
	public const uint PinnedObjectHeap = 64;
}