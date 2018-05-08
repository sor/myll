parser grammar MyllParser;

options { tokenVocab = MyllLexer; }

comment		:	COMMENT;

postOP		:	v=('++' |	'--');
preOP		:	v=('++' |	'--' |	'+'  |	'-'  |	'!'  |	'~'  | '*' | '&');

powOP		:			'*''*';
multOP		:	v=(				'*'  |	'/'  |	'%' |   '&');
addOP		:	v=(										'+' |   '-' |   '|' |   '^');
shiftOP		: 	'<<' |	'>''>';
/*
bitAndOP	:					'&';
bitXorOP	:							'^';
bitOrOP		:									'|';
*/
cmpOp       :   '<=>';
orderOP		:	v=('<'|'<='|'>'|'>=');
equalOP		:	v=('=='|'!=');

andOP		:					'&&';
orOP		:									'||';
//nulCondMemOP:	'?.';
nulCondIdxOP:	'?[';
nulCoalOP	:	'??';

memAccOP	:	v=('.'  | '?.'|'->');
memAccPtrOP	:	v=('.*' | '->*');

assignOP	:	v=(	'='  |	'**=' |	'*=' |	'/=' |	'%=' |	'+=' |	'-='
				|	'<<='|  '>>=' |	'&=' |	'^=' |	'|=');

lit			:	HEX_LIT | INTEGER_LIT | FLOAT_LIT | STRING_LIT | CHAR_LIT | BOOL_LIT | NUL;
wildId		:	AUTOINDEX | USCORE;
id			:	ID;
idOrLit		:	id | lit;

// +++ handled
specialType	:	v=( AUTO | VOID | BOOL );
charType	:	v=( CHAR | CODEPOINT | STRING );
floatingType:	v=( FLOAT | F80 | F64 | F32 | F16 );
binaryType	:	v=( BYTE | B64 | B32 | B16 | B8 );
signedIntType:	v=( INT  | ISIZE | I64 | I32 | I16 | I8 );
unsignIntType:  v=( UINT | USIZE | U64 | U32 | U16 | U8 );

basicType	:	specialType
			|	charType
			|	floatingType
			|	binaryType
			|	signedIntType
			|	unsignIntType;

typeQual	:	qual=( CONST | MUTABLE | VOLATILE | STABLE );
typeQuals	:	typeQual*;

typePtr		:	typeQuals	( ptr=( AT_BANG | AT_QUEST | AT_PLUS | DBL_AMP | AMP | STAR | PTR_TO_ARY )
							| ary=( AT_LBRACK | LBRACK ) expr? RBRACK )
			;

idTplArgs	:	id tplArgs?;
nestedType	:	idTplArgs (SCOPE idTplArgs)*;

funcType	:	FUNC tplArgs? funcDef (RARROW typeSpec)?;

typeSpec	:	typeQuals	( basicType | funcType | nestedType )	typePtr*;
// --- handled

arg			:	(id COLON)? expr;
funcCall	:	LPAREN	(arg (COMMA arg)*)?	RPAREN;
indexCall	:	ary=( QM_LBRACK | LBRACK)	(arg (COMMA arg)*)	RBRACK;

param		:	typeSpec id?;
funcDef		:	LPAREN (param (COMMA param)*)? RPAREN;

// this expr needs to be a constexpr and can be an id (from a surrounding template)
// TODO evaluate if 'id' is really necessary/beneficial here or just let expr handle it
tplArg		:	typeSpec | id | expr; //INTEGER_LIT | id;
tplArgs		:	LT tplArg (COMMA tplArg)* GT;

tplParams	:	LT id (COMMA id)* GT;

// Tier 3
//cast_MOD:	'c'|'s'|'d'|'r'|;
preOpExpr	:	preOP					expr;
castExpr	:	LPAREN typeSpec RPAREN	expr;
sizeofExpr	:	SIZEOF					expr;
newExpr		:	NEW		typeSpec?	funcCall?;
deleteExpr	:	DELETE	(ary='['']')?	expr;

expr		:	(idTplArgs	SCOPE)+		expr	# ScopedExpr
			|	expr
				(	postOP
				|	funcCall
				|	indexCall
				|	memAccOP	idTplArgs)		# PostExpr
			| <assoc=right>
				(	preOpExpr
				|	castExpr
				|	sizeofExpr
				|	newExpr
				|	deleteExpr	)				# PreExpr
			|	expr	memAccPtrOP		expr	# MemPtrExpr
			| <assoc=right>
				expr	powOP			expr	# PowExpr
			|	expr	multOP			expr	# MultExpr
			|	expr	addOP			expr	# AddExpr
			|	expr	shiftOP			expr	# ShiftExpr
			|	expr	cmpOp			expr	# ComparisonExpr
			|	expr	orderOP			expr	# RelationExpr
			|	expr	equalOP			expr	# EqualityExpr
			|	expr	andOP			expr	# AndExpr
			|	expr	orOP			expr	# OrExpr
			|	expr	nulCoalOP		expr	# NullCoalesceExpr
			| <assoc=right>
				expr	'?' expr ':'	expr	# ConditionalExpr
			|	LPAREN	expr	RPAREN			# ParenExpr
			|	wildId							# WildIdExpr
			|	lit								# LiteralExpr
			|	idTplArgs						# IdTplExpr
			;
/*
exprOld		:	expr	SCOPE			expr	# Tier1
			|	expr
				(	postOP
				// func cast
				|	funcCall
				|	indexCall
				|	memOP	id	)				# Tier2
			| <assoc=right>
				(	preOpExpr
				|	castExpr
				|	sizeofExpr
				|	newExpr
				|	deleteExpr	)				# Tier3
			|	expr	memPtrOP		expr	# Tier4
			| <assoc=right>
				expr	powOP			expr	# Tier4_5
			|	expr	multOP			expr	# Tier5
			|	expr	addOP			expr	# Tier6
			|	expr	shiftOP			expr	# Tier7
			|	expr	orderOP			expr	# Tier8
			|	expr	equalOP			expr	# Tier9
			|	expr	bitAndOP		expr	# Tier10
			|	expr	bitXorOP		expr	# Tier11
			|	expr	bitOrOP			expr	# Tier12
			|	expr	andOP			expr	# Tier13
			|	expr	orOP			expr	# Tier14
			| <assoc=right>
				expr	( assignOP
						| '?' expr ':')	expr	# Tier15
			|			'throw'			expr	# Tier16
			//|	expr	','				expr	# Tier17
			|	'('		expr	')'				# ParenExpr
			|	wildId							# Tier50
			|	lit								# Tier51
			|	idTplArgs						# Tier52
			;
*/

idExpr		:	id (ASSIGN expr)?;
// TODO: Prop - get,set,refget
typedIdExprs:	typeSpec idExpr (COMMA idExpr)*;

attrib		:	id	(	'=' idOrLit
					|	'(' idOrLit (COMMA idOrLit)* ')'
					)?;
attribBlk	:	LBRACK attrib (COMMA attrib)* RBRACK;

stmtDef		:	USING	nestedType (COMMA nestedType)*	SEMI				# Using
			|	VAR		typedIdExprs SEMI									# VariableDecl
			|	CONST	typedIdExprs SEMI									# VariableDecl
			;
stmt		:	stmtDef														# StmtDecl
			|	RETURN	expr	SEMI										# ReturnStmt
			|	THROW	expr	SEMI										# ThrowStmt
			|	BREAK			SEMI										# BreakStmt
			|	FALL			SEMI										# FallStmt
			|	IF	LPAREN expr RPAREN stmt ( ELSE stmt )?					# IfStmt
			|	FOR	LPAREN stmt expr SEMI expr RPAREN stmt ( ELSE stmt )?	# ForStmt
			|	expr TIMES id?		stmt									# TimesStmt
			|	expr '..' expr		stmt									# EachStmt
			| 	(expr	assignOP)+	expr	SEMI							# AssignmentStmt
			|	stmtBlk														# BlockStmt
			|	expr SEMI													# ExpressionStmt
			;
stmtBlk		:	LCURLY	stmt*	RCURLY;

classDef	:	(PUB | PRIV | PROT) COLON						# AccessMod
			|	CTOR	ctorDecl								# ClassCtorDecl
			|	ALIAS 	id ASSIGN typeSpec SEMI					# Alias
			|	STATIC	LCURLY classExtDef* RCURLY				# StaticDecl
			|	classExtDef										# ClassExtendedDecl
			;

classExtDef	:	FIELDS	LCURLY (typedIdExprs	SEMI)* RCURLY
			|	FIELD			typedIdExprs	SEMI
			|	PROP			typedIdExprs	SEMI
			|	METH			funcDecl
			|	OPERATOR		opDecl
			;

initList	:	COLON id funcCall (COMMA id funcCall)*;
ctorDecl	:	funcDef initList?	stmtBlk						# CtorDef;

funcDecl	:	id tplParams?	funcDef (RARROW typeSpec)?	(stmt|'=>' expr SEMI);
opDecl		:	STRING_LIT		funcDef (RARROW typeSpec)?	(stmt|'=>' expr SEMI);

topLevel	:	attribBlk																# Attributes
			|	NS	id (SCOPE id)*		SEMI											# Namespace
			|	NS	id (SCOPE id)*		LCURLY	topLevel+	RCURLY						# Namespace
			|	CLASS	id tplParams?	LCURLY	classDef*	RCURLY						# ClassDecl
			|	STRUCT	id tplParams?	LCURLY	classDef*	RCURLY						# StructDecl
			|	UNION	id tplParams?	LCURLY	classDef*	RCURLY						# UnionDecl
			|	ENUM	id				LCURLY	idExpr (COMMA idExpr)* COMMA? RCURLY	# EnumDecl
			|	FUNC	funcDecl														# FunctionDecl
			|	stmtDef																	# restStmt
			;

prog		:	topLevel+;
