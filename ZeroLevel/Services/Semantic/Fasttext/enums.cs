namespace ZeroLevel.Services.Semantic.Fasttext
{
    public enum model_name : int { cbow = 1, sg, sup };
    public enum loss_name : int { hs = 1, ns, softmax };
    public enum entry_type : byte { word=0, label=1};
}
