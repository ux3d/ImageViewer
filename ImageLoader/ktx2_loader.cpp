#include "ktx2_loader.h"

void ktx2_to_opengl_format(int gliFormat, int& glInternal, int& glExternal, int& glType, bool& isCompressed, bool& isSrgb)
{
	// TODO: Implement.
}

std::unique_ptr<ImageResource> ktx2_load(const char* filename)
{
	std::unique_ptr<ImageResource> res = std::make_unique<ImageResource>();

	// TODO: Implement.

	return res;
}

void ktx2_create_storage(int format, int width, int height, int layer, int levels)
{
	// TODO: Implement.
}

void ktx2_store_level(int layer, int level, const void* data, uint64_t size)
{
	// TODO: Implement.
}

void ktx2_get_level_size(int level, uint64_t& size)
{
	// TODO: Implement.
}

void ktx2_save(const char* filename)
{
	// TODO: Implement.
}
