namespace Tracking4All
{
    /// <summary>
    /// The minimum settings required for solutions.
    /// </summary>
    public interface ISolutionSettings
    {
        /// <summary>
        /// The width of the input image(s) for the solution.
        /// </summary>
        public int Width { get; }
        /// <summary>
        /// The height of the input image(s) for the solution.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// If true, use the cameras perspective. If false, use the players perspective.
        /// </summary>
        public bool PerspectiveFlip { get; }
        /// <summary>
        /// If true, flip right and left of the pose.
        /// </summary>
        public bool Mirror { get; }
    }
}