namespace DotnetFastestMemoryPacker.Internal;

enum GCAllocFlags : uint
{
	NoFlags          = 0x00,
	ZeroingOptional  = 0x10,
	PinnedObjectHeap = 0x40
}