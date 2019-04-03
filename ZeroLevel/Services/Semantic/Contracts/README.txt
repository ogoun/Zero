The implementation of the basis for semantic work with the text.

LexProvider - implements the selection of tokens from the text, where a token is any coercion of a word.
For example, a token can be directly the word itself, a system, a lemma.

Two factories were created as an implementation:

SnowbolLexProviderFactory - returns providers based on stemming 'Snowball'
JustWordLexProviderFactory - returns a provider that takes the word itself for the token, no change (lower case)

To implement your own provider, you need to create a class based on the ILexer interface and implement the Lex method,
in which the necessary normalization of the word in the necessary semantic context will be carried out.

For example:
public class LemmaLexer: ILexer
{
public string Lex (string word) {return Lemmatizer.Lemma (word); }
}

Then you can create a provider based on it:

var provider = new LexProvider (new LemmaLexer ());