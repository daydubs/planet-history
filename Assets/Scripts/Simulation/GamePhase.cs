namespace LifeStory.Core
{
    /// <summary>
    /// Phase actuelle du jeu
    /// </summary>
    public enum GamePhase
    {
        Infernal,       // Phase magma - pas de croûte (surface >900°C)
        Geological,    // Phase de formation planétaire
        Evolution,     // Phase d'évolution de la vie
        Paused         // Jeu en pause
    }

    /// <summary>
    /// Conditions climatiques globales
    /// </summary>
    public enum ClimateState
    {
        Frozen,        // Planète gelée
        Cold,          // Froid
        Temperate,     // Tempéré
        Warm,          // Chaud
        Hot,           // Très chaud
        Hellish        // Invivable
    }

    /// <summary>
    /// Composition atmosphérique dominante
    /// </summary>
    public enum AtmosphereType
    {
        None,          // Pas d'atmosphère
        Toxic,         // Toxique (méthane, CO2)
        Reducing,      // Réductrice (hydrogène, méthane)
        Oxidizing,     // Oxydante (oxygène)
        Balanced       // Équilibrée pour la vie
    }

    /// <summary>
    /// Types de terrain
    /// </summary>
    public enum TerrainType
    {
        Ocean,         // Océan
        Shallow,       // Eau peu profonde
        Beach,         // Plage
        Plains,        // Plaines
        Hills,         // Collines
        Mountains,     // Montagnes
        Volcanic,      // Zone volcanique
        Desert,        // Désert
        Tundra,        // Toundra
        Ice            // Glace
    }

    /// <summary>
    /// Niveaux d'évolution de la vie
    /// </summary>
    public enum LifeStage
    {
        None,          // Pas de vie
        Microbial,     // Vie microbienne
        Simple,        // Organismes simples
        Complex,       // Organismes complexes
        Intelligent,   // Vie intelligente
        Technological  // Civilisation technologique
    }

    /// <summary>
    /// Types d'événements géologiques
    /// </summary>
    public enum GeologicalEvent
    {
        VolcanicEruption,
        Earthquake,
        MeteorImpact,
        IceAge,
        Flooding,
        Drought,
        ContinentalDrift
    }

    /// <summary>
    /// Échelles de temps pour les événements
    /// </summary>
    public enum TimeScale
    {
        Instant,       // Événement immédiat
        Years,         // Quelques années
        Centuries,     // Siècles
        Millennia,     // Millénaires
        Geological     // Temps géologique (millions d'années)
    }
    /// <summary>
    /// États de l'eau sur la planète
    /// </summary>
    public enum WaterState
    {
        Vapor,      // Vapeur d'eau (trop chaud)
        Liquid,     // Eau liquide (océans)
        Ice,        // Glace (trop froid)
        Mixed       // Mélange (zones tempérées)
    }

    public enum AtmosphereComposition
    {
        None,           // Pas d'atmosphère
        Primitive,      // N₂ + CH₄ dominant
        Reducing,       // N₂ + CO₂ dominant  
        Oxidizing,      // N₂ + O₂ (phase Evolution)
        Balanced        // Atmosphère terrestre
    }
}