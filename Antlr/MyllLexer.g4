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

COMMENT		:	(	'#!' ~('\r'|'\n')*	// ignore shebang for now
				|	'//' ~('\r'|'\n')*
				|	'/*' .*? '*/'
				) -> channel(COMMENTS);

STRING_LIT	: '"' (STR_ESC | ~('\\' | '"'  | '\r' | '\n'))* '"';
CHAR_LIT	: '\'' (CH_ESC | ~('\\' | '\'' | '\r' | '\n')) '\'';
fragment STR_ESC: '\\' ('\\' | '"' | 't' | 'n' | 'r');
fragment CH_ESC:  '\\' ('\\' | '\'' | 't' | 'n' | 'r');

ARROW_STAR	: '->*';
POINT_STAR	: '.*';
PTR_TO_ARY	: '[*]';
COMPARE		: '<=>';
TRP_POINT	: '...';
DBL_POINT	: '..';
DBL_LBRACK	: '[[';
DBL_RBRACK	: ']]';
DBL_AMP		: '&&';
DBL_QM		: '??';
QM_COLON	: '?:';
//DBL_STAR	: '**';	// this is only supported by 2x STAR because of: var int** a; which is a double pointer, not a pow
DBL_PLUS	: '++';
DBL_MINUS	: '--';
RARROW		: '->';
PHATRARROW	: '=>';
MOVEARROW	: '=<';
LSHIFT		: '<<';
//RSHIFT	: '>>';// this is only supported by 2x GT because of: var v<v<int>> a; which is two templates closing, not a right shift
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
QM_LPAREN	: '?(';
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
QM_POINT_STAR: '?.*';
QM_POINT	: '?.';
QM_LBRACK	: '?[';
DOT			: '·';
CROSS		: '×';
DIV			: '÷';
POINT		: '.';
EM			: '!';
TILDE		: '~';
DBL_PIPE	: '||';
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
AS_SLASH	: '/=';
AS_MOD		: '%=';
AS_DOT		: '·=';
AS_CROSS	: '×=';
AS_DIV		: '÷=';
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
PUB			: 'public'|'pub';
PRIV		: 'private'|'priv';
PROT		: 'protected'|'prot';
USING		: 'using';
ALIAS       : 'alias';
UNION		: 'union';
STRUCT		: 'struct';
CLASS		: 'class';
CTOR		: 'ctor';
COPYCTOR	: 'copyctor'|'copy_ctor';
MOVECTOR	: 'movector'|'move_ctor';
DTOR		: 'dtor';
COPYASSIGN	: 'copy=';
MOVEASSIGN	: 'move=';
FUNC		: 'func';
PROC		: 'proc';
METHOD		: 'method';
ENUM		: 'enum';
CONCEPT		: 'concept';
REQUIRES	: 'requires';
PROP		: 'prop';
GET			: 'get';
REFGET		: 'refget';
SET			: 'set';
FIELD		: 'field';
OPERATOR	: 'operator';
VAR			: 'var';
LET			: 'let';
LOOP		: 'loop';
FOR			: 'for';
DO			: 'do';
WHILE		: 'while';
TIMES		: 'times';
IF			: 'if';
ELSE		: 'else';
SWITCH		: 'switch';
CASE		: 'case';
BREAK		: 'break';
FALL		: 'fall';
RETURN		: 'return';
SIZEOF		: 'sizeof';
NEW			: 'new';
DELETE		: 'delete';
THROW		: 'throw';

ID			: ALPHA_ ALNUM_*;

NUL			: ('null'|'nullptr');
CLASS_LIT	: 'this'|'base';
BOOL_LIT	: 'true'|'false';
FLOAT_LIT	:	(	DIGIT* '.' DIGIT+ ( [eE] [+-]? DIGIT+ )?
				|	DIGIT+ [eE] [+-]? DIGIT+
				) [lfLF]?;
HEX_LIT     : '0x' HEXDIGIT HEXDIGIT_*;
OCT_LIT     : '0o' OCTDIGIT OCTDIGIT_*;
BIN_LIT     : '0b' BINDIGIT BINDIGIT_*;
INTEGER_LIT	: (DIGITNZ DIGIT_*|[0]);

fragment DIGITNZ	: [1-9];
fragment DIGIT		: [0-9];
fragment DIGIT_		: [0-9_];
fragment HEXDIGIT	: [0-9A-Fa-f];
fragment HEXDIGIT_	: [0-9A-Fa-f_];
fragment OCTDIGIT	: [0-7];
fragment OCTDIGIT_	: [0-7_];
fragment BINDIGIT	: [0-1];
fragment BINDIGIT_	: [0-1_];
fragment ALPHA_		: [A-Za-z_];
fragment ALNUM_		: [0-9A-Za-z_];

NL			: ('\r'|'\n')+	-> channel(NEWLINES);
WS			: (' '|'\t')+	-> skip;// channel(HIDDEN);

/*
mode MODE;
LOOK		: 'look';
COPY		: 'copy';
EDIT		: 'edit';
SHARE		: 'share';
WEAKSHARE	: 'weakshare';
CONSUME		: 'consume';
PRODUCE		: 'produce';
REPLACE		: 'replace';
*/
