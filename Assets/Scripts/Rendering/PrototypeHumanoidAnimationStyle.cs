namespace LaneSurvivor.Rendering
{
    public enum PrototypeHumanoidAnimationStyle
    {
        // Survivor rigs walk only when their gameplay root moves.
        SurvivorSquad,

        // Basic zombies shamble in place because their gameplay roots are stationary lane threats.
        ZombieShamble,

        // Armored zombies use a heavier in-place shamble.
        ArmoredZombieShamble
    }
}
