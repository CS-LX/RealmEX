using Engine;

namespace RealmEX.Presets.Ponder
{
    public enum RealmPonderActorKind { Item, Model }
    /// <summary>Scripted items and models share the deterministic scene clock.</summary>
    public sealed class RealmPonderActor
    {
        public string Id { get; }
        public RealmPonderActorKind Kind { get; }
        public int ItemValue { get; internal set; }
        public string ModelAsset { get; }
        public string TextureAsset { get; }
        public Vector3 Position { get; internal set; }
        public Vector3 Rotation { get; internal set; }
        public float Scale { get; internal set; } = 1;
        public Matrix Transform => Matrix.CreateScale(Scale) * Matrix.CreateRotationX(MathUtils.DegToRad(Rotation.X))
            * Matrix.CreateRotationY(MathUtils.DegToRad(Rotation.Y)) * Matrix.CreateRotationZ(MathUtils.DegToRad(Rotation.Z)) * Matrix.CreateTranslation(Position);
        internal RealmPonderActor(string id, RealmPonderActorKind kind, Vector3 position, int itemValue, string modelAsset, string textureAsset)
        { Id = id; Kind = kind; Position = position; ItemValue = itemValue; ModelAsset = modelAsset; TextureAsset = textureAsset; }
    }
}
