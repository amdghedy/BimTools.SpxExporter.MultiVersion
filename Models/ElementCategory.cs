using System;

namespace BimTools.SpxExporter.Models
{
    /// <summary>
    /// Represents high‑level element categories for export filtering.
    /// </summary>
    public enum ElementCategory
    {
        All,
        /// <summary>Plywood elements that touch concrete (e.g., formwork).</summary>
        PlywoodTouchingConcrete,
        Joists,
        Joints,
        Primary,
        Horizontal,
        // Extend with additional categories as needed.
    }
}
