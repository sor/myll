parser grammar MyllParser;

options { tokenVocab = MyllLexer; }

comment		:	COMMENT;

postOP		:	v=('++' |	'--');

// handled by ToPreOp because of colisions
preOP		:	v=('++' |	'--' |	'+'  |	'-'  |	'!'  |	'~'  | '*' | '&');

powOP		:			'*''*';
multOP		:	v=(				'*'  |	'/'  |	'%' |   '&' | '·' | '×' | '÷');
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

nulCoalOP	:	'??';

memAccOP	:	v=('.'  | '?.'	| '->'	);
memAccPtrOP	:	v=('.*' | '?.*'	| '->*'	);

assignOP	:	'=';
aggrAssignOP:	v=(			'**=' |	'*=' |	'/=' |	'%=' |	'+=' |	'-='
				|	'<<='|  '>>=' |	'&=' |	'^=' |	'|=');

lit			:	HEX_LIT | OCT_LIT | BIN_LIT | INTEGER_LIT | FLOAT_LIT | STRING_LIT | CHAR_LIT | BOOL_LIT | NUL;
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
nestedTypes	:	nestedType (COMMA nestedType)* COMMA?;

funcType	:	FUNC tplArgs? funcTypeDef (RARROW typeSpec)?;

typeSpec	:	typeQuals	( basicType | funcType | nestedType )	typePtr*;
// --- handled

arg			:	(id COLON)? expr;
args		:	(arg (COMMA arg)* COMMA?);
funcCall	:	ary=( QM_LPAREN | LPAREN )	args?	RPAREN;
indexCall	:	ary=( QM_LBRACK | LBRACK )	args	RBRACK;

param		:	typeSpec id?;
funcTypeDef	:	LPAREN (param (COMMA param)*)? RPAREN;

// this expr needs to be a constexpr and can be an id (from a surrounding template)
// TODO evaluate if 'id' is really necessary/beneficial here or just let expr handle it
tplArg		:	typeSpec | id | expr; //INTEGER_LIT | id;
tplArgs		:	LT tplArg (COMMA tplArg)* GT;

tplParams	:	LT id (COMMA id)* GT;

// Tier 3
//cast: nothing = static, ? = dynamic, ! = const & reinterpret
preOpExpr	:	preOP					expr;
castExpr	:	LPAREN (QM|EM)? typeSpec RPAREN	expr;
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

idAccessor	:	id	(LCURLY accessorDef+ RCURLY)?	(ASSIGN expr)?;
idExpr		:	id									(ASSIGN expr)?;
idAccessors	:	idAccessor	(COMMA idAccessor)*	COMMA?;
idExprs		:	idExpr		(COMMA idExpr)*		COMMA?;
typedIdAcors:	typeSpec 	idAccessors	SEMI;
typedIdExprs:	typeSpec 	idExprs		SEMI;

attrib		:	id	(	'=' idOrLit
					|	'(' idOrLit (COMMA idOrLit)* ')'
					)?;
attribBlk	:	LBRACK	attrib (COMMA attrib)* COMMA? RBRACK;

caseStmt	:	CASE expr (COMMA expr)* COLON levStmt+ (FALL SEMI)?;

initList	:	COLON id funcCall (COMMA id funcCall)* COMMA?;
ctorDef		:	funcTypeDef	initList?	(SEMI | levStmt);

funcBody	:	('=>' expr SEMI | levStmt);
accessorDef	:	CONST?			v=(GET | REFGET | SET)			funcBody;
funcDef		:	id tplParams?	funcTypeDef (RARROW typeSpec)?	funcBody;
opDef		:	STRING_LIT		funcTypeDef (RARROW typeSpec)?	funcBody;


prog		:	levTop+;

// only refer to these lev*, not the in*
levTop		:	attribBlk? ( inAnyStmt | inAnyDecl | inTop );
levClass	:	attribBlk? ( inAnyStmt | inAnyDecl | inClass );
levStmt		:	attribBlk? ( inAnyStmt | inStmt );
levStmtDef	:	attribBlk? ( inAnyStmt );

// ns
inTop		:	NS	id (SCOPE id)*	SEMI					# Namespace
			|	NS	id (SCOPE id)*	LCURLY	levTop+	RCURLY	# Namespace
			;

// class, enum, func
inAnyDecl	:	v=(CLASS | STRUCT | UNION) id tplParams?
				(COLON	nestedTypes)?
						LCURLY	levClass*	RCURLY	# ClassDecl
			|	ENUM id	LCURLY	idExprs		RCURLY	# EnumDecl
			|	FUNC	LCURLY	funcDef*	RCURLY	# FunctionDecl
			|	FUNC			funcDef				# FunctionDecl
			|	OPERATOR LCURLY	opDef		RCURLY	# OpDecl
			|	OPERATOR		opDef				# OpDecl
			;

// ppp, prop, ctor, alias, static
inClass		:	v=(PUB | PRIV | PROT) COLON			# AccessMod
//			|	PROP	typedIdExprs				# PropDecl
			|	CTOR	ctorDef						# CtorDecl
			|	DTOR	ctorDef						# CtorDecl
			|	ALIAS 	id ASSIGN typeSpec SEMI		# Alias
			|	STATIC	LCURLY	levClass* RCURLY	# StaticDecl
			|	STATIC			levClass			# StaticDecl
			;

// using, var, const
inAnyStmt	:	USING	nestedTypes	SEMI				# Using
			|	VAR		LCURLY	typedIdAcors* RCURLY	# VariableDecl
			|	VAR				typedIdAcors			# VariableDecl
			|	CONST	LCURLY	typedIdExprs* RCURLY	# VariableDecl
			|	CONST			typedIdExprs			# VariableDecl
			;

inStmt		:	SEMI								# EmptyStmt
			|	RETURN	expr?	SEMI				# ReturnStmt
			|	THROW	expr	SEMI				# ThrowStmt
			|	BREAK	INTEGER_LIT	SEMI			# BreakStmt
			|	IF	LPAREN expr RPAREN
				levStmt	(ELSE levStmt)?				# IfStmt
			|	SWITCH LPAREN expr RPAREN	LCURLY
				caseStmt+ 	(ELSE levStmt+)? RCURLY	# SwitchStmt
			|	LOOP	levStmt						# LoopStmt
			|	FOR LPAREN levStmtDef expr SEMI expr RPAREN
				levStmt	(ELSE levStmt)?				# ForStmt
			|	WHILE LPAREN expr RPAREN
				levStmt	(ELSE levStmt)?				# WhileStmt
			|	DO levStmt
				WHILE LPAREN expr RPAREN SEMI?		# DoWhileStmt
			|	expr TIMES			id?	levStmt		# TimesStmt
			|	expr DBL_POINT expr id?	levStmt		# EachStmt
			| 	(expr	assignOP)+		expr SEMI	# MultiAssignStmt
			| 	expr	aggrAssignOP	expr SEMI	# AggrAssignStmt
			|	LCURLY	levStmt*	RCURLY			# BlockStmt
			|	expr SEMI							# ExpressionStmt
			;
