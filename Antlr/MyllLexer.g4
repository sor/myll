lexer grammar MyllLexer;

/*
tokens
{
	NEWLINES,
	COMMENTS
}
*/

channels { NEWLINES, COMMENTS }

/*@lexer::members
{
	protected const int EOF			= Eof;
	protected const int HIDDEN		= Hidden;
	protected const int NEWLINES	= Hidden;
	protected const int COMMENTS	= Hidden;
}*/

COMMENT		: ( '//' ~('\r'|'\n')*
			  | '/*' .*? '*/'
			  )	-> channel(COMMENTS);

STRING_LIT	: '"' (STR_ESC | ~('\\' | '"'  | '\r' | '\n'))* '"';
CHAR_LIT	: '\'' (CH_ESC | ~('\\' | '\'' | '\r' | '\n')) '\'';
fragment STR_ESC: '\\' ('\\' | '"' | 't' | 'n' | 'r');
fragment CH_ESC:  '\\' ('\\' | '\'' | 't' | 'n' | 'r');

ARROW_STAR	: '->*';
DOT_STAR	: '.*';
PTR_TO_ARY	: '[*]';
TRP_POINT	: '...';
DBL_POINT	: '..';
DBL_LBRACK	: '[[';
DBL_RBRACK	: ']]';
DBL_AMP		: '&&';
//DBL_STAR	: '**';
DBL_PLUS	: '++';
DBL_MINUS	: '--';
RARROW		: '->';
PHATRARROW	: '=>';
LSHIFT		: '<<';
//RSHIFT		: '>>';
SCOPE		: '::';
AT_BANG		: '@!';
AT_QUEST	: '@?';
AT_PLUS		: '@+';
AT_LBRACK	: '@[';
AUTOINDEX	: '#' DIGIT;
LBRACK		: '[';
RBRACK		: ']';
LCURLY		: '{';
RCURLY		: '}';
LPAREN		: '(';
RPAREN		: ')';
AT			: '@';
AMP			: '&';
STAR		: '*';
SLASH		: '/';
MOD			: '%';
PLUS		: '+';
MINUS		: '-';
SEMI		: ';';
COLON		: ':';
COMMA		: ',';
POINT		: '.';
EXCL		: '!';
TILDE		: '~';
PIPEPIPE	: '||';
PIPE		: '|';
QM			: '?';
HAT			: '^';
USCORE		: '_';

EQ			: '==';
NEQ			: '!=';
LTEQ		: '<=';
GTEQ		: '>=';
LT			: '<';
GT			: '>';

ASSIGN		: '=';
AS_POW		: '**=';
AS_MUL		: '*=';
AS_DIV		: '/=';
AS_MOD		: '%=';
AS_ADD		: '+=';
AS_SUB		: '-=';
AS_LSH		: '<<=';
AS_RSH		: '>>=';
AS_AND		: '&=';
AS_OR		: '|=';
AS_XOR		: '^=';

AUTO		: 'auto';
VOID		: 'void';
BOOL		: 'bool';
INT			: 'int';
UINT		: 'uint';
ISIZE		: 'isize';
USIZE		: 'usize';
BYTE		: 'byte';
CHAR		: 'char';
CODEPOINT	: 'codepoint';
STRING		: 'string';
HALF		: 'half';
FLOAT		: 'float';
DOUBLE		: 'double';
LONGDOUBLE	: 'longdouble';

I64			: 'i64';
I32			: 'i32';
I16			: 'i16';
I8			: 'i8';
U64			: 'u64';
U32			: 'u32';
U16			: 'u16';
U8			: 'u8';
B64			: 'b64';
B32			: 'b32';
B16			: 'b16';
B8			: 'b8';
F80			: 'f80';
F64			: 'f64';
F32			: 'f32';
F16			: 'f16';

NS			: 'namespace';
VOLATILE	: 'volatile';
STABLE		: 'stable';
CONST		: 'const';
MUTABLE		: 'mutable';
STATIC		: 'static';
USING		: 'using';
ALIAS       : 'alias';
UNION		: 'union';
STRUCT		: 'struct';
CLASS		: 'class';
CTOR		: ('constructor'|'ctor');
DTOR		: ('destructor'|'dtor');
PUB			: ('public'|'pub');
PRIV		: ('private'|'priv');
PROT		: ('protected'|'prot');
FUNC		: ('function'|'func'|'fn');
METH		: ('method'|'meth');
ENUM		: 'enum';
PROP		: 'prop';
FIELDS		: 'fields';
FIELD		: 'field';
OPERATOR	: 'operator';
VAR			: 'var';
LET			: 'let';
FOR			: 'for';
TIMES		: 'times';
IF			: 'if';
ELSE		: 'else';
BREAK		: 'break';
RETURN		: 'return';
SIZEOF		: 'sizeof';
NEW			: 'new';
DELETE		: 'delete';
THROW		: 'throw';

NUL			: 'null';
BOOL_LIT	: 'true'|'false';
ID			: [a-zA-Z_][a-zA-Z0-9_]*;
FLOAT_LIT	:	(	DIGIT* '.' DIGIT+ ( [eE] [+-]? DIGIT+ )?
				|	DIGIT+ [eE] [+-]? DIGIT+ 
				) [lfLF]?;
HEX_LIT     : '0x' DIGIT+;
INTEGER_LIT	: DIGIT+;
fragment DIGIT: [0-9];
NL			: ('\r'|'\n')+	-> channel(NEWLINES);
WS			: (' '|'\t')+	-> skip;// channel(HIDDEN);

