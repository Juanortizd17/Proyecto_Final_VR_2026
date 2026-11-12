namespace MixLab
{
    public enum Temperature { Ambiente, Frio, Caliente }

    public enum ReactionId
    {
        Perfecto, Picante, Infierno, Explosion, Borracho, Alegre,
        Nausea, Congelado, Hiper, Llamarada, Desmayo, SinChiste, Azucar,
        Hipo, Agrio, Dormido, Bailarin
    }

    public enum MixState { Onboarding, Playing, Finished }

    /// <summary>Clasificación de la mezcla que hace el analizador de la estación antes de servir.</summary>
    public enum MixClass { Vacia, Estable, Inestable, Toxica }
}
