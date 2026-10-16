// MPUImageSourceAdapter
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public class MPUImageSourceAdapter : ProxyAdapter<ImageFrameIndex, ImageFrame, ImageFrameIndex, MPUImageSource>
    {
        public MPUImageSourceAdapter(int groupSize, IProvider<ImageFrameIndex, ImageFrame> dataSource, float aliveThreshold = -1)
            : base(groupSize, dataSource, aliveThreshold)
        {
        }

        protected override void SelfReady(int group)
        {
            base.SelfReady(group);

            MPUImageSource source = Get(group, 0);
            source.Hook(DataSource.Get(group, 0));
        }
        protected override void SelfDispose(int group)
        {
            base.SelfDispose(group);

            MPUImageSource source = Get(group, 0);
            source.Dispose();
        }

        protected override void SelfUpdate(int group, IProvider<ImageFrameIndex, ImageFrame> from)
        {

        }
    }
}