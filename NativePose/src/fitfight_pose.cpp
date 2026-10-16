#include <net.h>
#include <vector>
#include <cstring>
#include <algorithm>
#include <memory>

// Model memory must outlive Net (memory-loaded weights can be referenced by layers).
struct PoseContext {
    std::vector<unsigned char> weights;
    ncnn::Net net;
};
#define API extern "C" __attribute__((visibility("default")))
API void* ff_pose_create(const unsigned char* param, int param_size,
                         const unsigned char* weights, int weights_size, int threads) {
    if (!param || !weights || param_size <= 0 || weights_size <= 0) return nullptr;
    auto ctx = std::make_unique<PoseContext>();
    ctx->weights.assign(weights, weights + weights_size);
    std::vector<char> text(param_size + 1, 0);
    std::memcpy(text.data(), param, param_size);
    ctx->net.opt.num_threads = std::max(1, std::min(threads, 4));
    ctx->net.opt.use_vulkan_compute = false;
    ctx->net.opt.use_fp16_packed = false;
    ctx->net.opt.use_fp16_storage = false;
    ctx->net.opt.use_fp16_arithmetic = false;
    // Extractor converts public outputs to pack1 while optimized internal layers remain packed.
    ctx->net.opt.use_packing_layout = true;
    if (ctx->net.load_param_mem(text.data()) != 0) return nullptr;
    if (ctx->net.load_model(ctx->weights.data()) <= 0) return nullptr;
    return ctx.release();
}
API int ff_pose_infer(void* handle, const float* input, float* x, float* y) {
    if (!handle || !input || !x || !y) return -1;
    auto ctx = static_cast<PoseContext*>(handle);
    ncnn::Mat image(192, 256, 3, const_cast<float*>(input));
    auto ex = ctx->net.create_extractor();
    if (ex.input("in0", image) != 0) return -2;
    ncnn::Mat outx, outy;
    if (ex.extract("out0", outx) != 0 || ex.extract("out1", outy) != 0) return -3;
    if (outx.w != 384 || outy.w != 512 || outx.h != 26 || outy.h != 26 || outx.c != 1 || outy.c != 1 || outx.elempack != 1 || outy.elempack != 1) return -4;
    for (int j=0;j<26;j++) {
        std::memcpy(x+j*384, outx.row(j), 384*sizeof(float));
        std::memcpy(y+j*512, outy.row(j), 512*sizeof(float));
    }
    return 0;
}
API void ff_pose_destroy(void* handle) { delete static_cast<PoseContext*>(handle); }
API const char* ff_pose_version() { return "ncnn 20260526 / RTMPose-s Halpe26 / CPU FP32"; }
