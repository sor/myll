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

ARROW_STAR	: '->*';
POINT_STAR	: '.*';
PTR_TO_ARY	: '[*]';	// Maybe a ptr to an array can just be "[]" (depending on context of course)?
COMPARE		: '<=>';	// <*>
TRP_POINT	: '...';
DBL_POINT	: '..';
DBL_AMP		: '&&';
DBL_QM		: '??';
QM_COLON	: '?:';
//DBL_STAR	: '**';	// this is only supported by 2x STAR because of: var int** a; which is a double pointer, not a pow
DBL_PLUS	: '++';
DBL_MINUS	: '--';
RARROW		: '->';	// also the "to-operator"   (not really an operator), read as "to"
//LARROW	: '<-';	//      the "from-operator" (not really an operator), read as "from"
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
BYTE		: 'byte'; // 8 bits, same as b8, not an integer, not a character
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
F128		: 'f128';	// (opt) long double prec. float
F64			: 'f64';	// double prec. float
F32			: 'f32';	// single prec. float
F16			: 'f16';	// (opt) half prec. float

// Arbitrary sized integers (as replacement for bitfields, ": 0" needs a different solution)
// only works in locals and Structural instance fields, CAN'T have a pointer taken (why though?)
//INT_ANY		: 'i' (DIGITNZ DIGIT*);
//UINT_ANY	: 'u' (DIGITNZ DIGIT*);
//BIN_ANY		: 'b' (DIGITNZ DIGIT*);

NAMESPACE	: 'namespace';
MODULE		: 'module';
IMPORT		: 'import';
VOLATILE	: 'volatile';
STABLE		: 'stable';			// antonym to volatile
CONST		: 'const';
MUTABLE		: 'mut'|'mutable';	// antonym to const
USING		: 'using';
ALIAS       : 'alias';
UNION		: 'union';
STRUCT		: 'struct';
CLASS		: 'class';
CTOR		: 'ctor';
DTOR		: 'dtor';
// == Exploration ==
// # Read as "convert from OTHER"
// explicit ctor( OTHER o )	{...}	// manual, converting ctor, convert from type OTHER
// convert <- OTHER o 		{...}	// is a ctor like above
// convert OTHER o ->		{...}	// same as above, alternative syntax
// ctor convert <- OTHER o	{...}	// same as above, with ctor
// ctor convert OTHER o ->	{...}	// same as above, alternative syntax
// ctor convert OTHER o		{...}	// same as above, no arrow syntax
//
// # Read as "convert to OTHER"
// explicit operator OTHER(){...}	// manual, converting operator, convert self to type OTHER
// convert -> OTHER			{...}	// is a convert operator like above, pure by default
// operator convert -> OTHER{...}	// same as "convert -> OTHER", alternative syntax
// operator convert OTHER	{...}	// same as above, no arrow syntax

// ctor()					{...}	// manual
// ctor default				{...}	// same as above
// ctor( const SELF & oth )	{...}	// manual
// ctor copy				{...}	// same as above
// ctor( SELF && other )	{...}	// manual
// ctor move				{...}	// same as above
// ctor forward : BASE		{...}	template <typename... Args, typename = decltype(BASE(std::declval<Args>()...))> SELF(Args&&... args) : BASE(std::forward<Args>(args)...) {}
//
// operator "=" ( const SELF & o )	{...}	// manual
// operator copy =					{...}
// operator "=" ( SELF && o )		{...}	// manual
// operator move =					{...}

FUNC		: 'func';//|'function;
PROC		: 'proc';//|'procedure';
METHOD		: 'meth'|'method';
ENUM		: 'enum';
ASPECT		: 'aspect';
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
DEFAULT		: 'default';
CASE		: 'case';
CONTINUE	: 'continue';
BREAK		: 'break';
FALL		: 'fall';
RETURN		: 'return';
TRY			: 'try';
CATCH		: 'catch';
DEFER		: 'defer';
SIZEOF		: 'sizeof';
NEW			: 'new';
DELETE		: 'delete';
THROW		: 'throw';
NOT			: 'not';
NAN			: 'nan';
INF			: 'inf';
IS			: 'is';

CONVERT		: 'conv'|'convert';
FORWARD		: 'forw'|'forward';
MOVE		: 'move';
COPY		: 'copy';

ID			: ALPHA_ ALNUM_*;

// The keywords "ret, result, other, that" are not really keywords,
//	they exist only in special spots and depending on context,
//	and they are handled by "id"
NUL			: 'null'|'nullptr'; // nullptr obsolete?
//CLASS_LIT	: 'this'|'self'|'base'|'super';
CLASS_LIT	: 'this'|'that'|'self'|'other'|'base'|'super'; // 'ret'?, maybe its not good that other etc are in here
BOOL_LIT	: 'true'|'false';
FLOAT_LIT	:	(	DIGIT* '.' DIGIT+ ( [eE] [+-]? DIGIT+ )?
				|	DIGIT+ [eE] [+-]? DIGIT+
				) [lfLF]?;
HEX_LIT		: '0x' HEXDIGIT HEXDIGIT_*;
OCT_LIT		: '0o' OCTDIGIT OCTDIGIT_*;
BIN_LIT		: '0b' BINDIGIT BINDIGIT_*;
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
mode PARAMETERS;
LOOK		: 'look';
COPY		: 'copy';
EDIT		: 'edit';
SHARE		: 'share';
WEAKSHARE	: 'weakshare';
CONSUME		: 'consume';
PRODUCE		: 'produce';
REPLACE		: 'replace';
*/
