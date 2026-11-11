namespace QualityControlXR
{
    /// <summary>
    /// Lo que el HUD de PC necesita saber del sistema de interacción activo
    /// (así el mismo HUD sirve para cualquier juego del proyecto).
    /// </summary>
    public interface IAimHud
    {
        /// <summary>Indicación contextual bajo la mira (rich text). Vacío = nada.</summary>
        string Prompt { get; }
        /// <summary>true si hay algo con lo que se puede interactuar (resalta la mira).</summary>
        bool HasTarget { get; }
        /// <summary>Barra de estado abajo a la derecha (rich text, hasta 2 líneas).</summary>
        string StatusText { get; }
        /// <summary>Texto del panel de controles (rich text).</summary>
        string HelpText { get; }
    }
}
