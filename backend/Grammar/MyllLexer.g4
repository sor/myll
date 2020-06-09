lexer grammar MyllLexer;

channels { NEWLINES, COMMENTS }

COMMENT		:	(	'#!' ~('\r'|'\n')*	// ignore shebang for now
				|	'//' ~('\r'|'\n')*
				|	'/*' .*? '*/'
				) -> channel(COMMENTS);

STRING_LIT	: '"' (STR_ESC | ~('\\' | '"'  | '\r' | '\n'))* '"';
CHAR_LIT	: '\'' (CH_ESC | ~('\\' | '\'' | '\r' | '\n')) '\'';
fragment STR_ESC: '\\' ('\\' | '"'  | 'a' | 'b' | 'f' | 'n' | 't' | 'r' | 'v');
fragment CH_ESC:  '\\' ('\\' | '\'' | 'a' | 'b' | 'f' | 'n' | 't' | 'r' | 'v');

MOVE		: '(move)';
ARROW_STAR	: '->*';
POINT_STAR	: '.*';
PTR_TO_ARY	: '[]*';	// [*] could be a dynamic array
COMPARE		: '<=>';	// <*>
TRP_POINT	: '...';
DBL_POINT	: '..';
DBL_AMP		: '&&';
DBL_QM		: '??';
QM_COLON	: '?:';
//DBL_STAR	: '**';	// this is only supported by 2x STAR because of: var int** a; which is a double pointer, not a pow
DBL_PLUS	: '++';
DBL_MINUS	: '--';
RARROW		: '->';
PHATRARROW	: '=>';
LSHIFT		: '<<';
//RSHIFT	: '>>';// this is only supported by 2x GT because of: var v<v<int>> a; which is two templates closing, not a right shift
SCOPE		: '::';
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
CODEPOINT	: 'codept'|'codepoint';
STRING		: 'string';
FLOAT		: 'float';

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
F128		: 'f128';	// long double prec. float
F64			: 'f64';	// double prec. float
F32			: 'f32';	// single prec. float
F16			: 'f16';	// half prec. float

NS			: 'namespace';
MODULE		: 'module';
IMPORT		: 'import';
VOLATILE	: 'volatile';
STABLE		: 'stable';
CONST		: 'const';
MUTABLE		: 'mut'|'mutable';
//STATIC		: 'static';
PUB			: 'pub'|'public';
PRIV		: 'priv'|'private';
PROT		: 'prot'|'protected';
USING		: 'using';
ALIAS       : 'alias';
UNION		: 'union';
STRUCT		: 'struct';
CLASS		: 'class';
CTOR		: 'ctor';
DTOR		: 'dtor';
COPYCTOR	: 'copyctor'|'copy_ctor';
MOVECTOR	: 'movector'|'move_ctor';
COPYASSIGN	: 'copy=';
MOVEASSIGN	: 'move=';
FUNC		: 'func';
PROC		: 'proc';
METHOD		: 'meth'|'method';
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
LET			: 'let';	// obsolete?
LOOP		: 'loop';
FOR			: 'for';
DO			: 'do';
WHILE		: 'while';
TIMES		: 'times';
IF			: 'if';
ELSE		: 'else';
SWITCH		: 'switch';
DEFAULT		: 'default';
CASE		: 'case';
BREAK		: 'break';
FALL		: 'fall';
RETURN		: 'return';
SIZEOF		: 'sizeof';
NEW			: 'new';
DELETE		: 'delete';
THROW		: 'throw';

ID			: ALPHA_ ALNUM_*;

NUL			: 'null'|'nullptr'; // nullptr obsolete?
CLASS_LIT	: 'this'|'self'|'base'|'super';
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
contextual keywords for parameters
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
