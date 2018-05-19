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

idExpr		:	id (ASSIGN expr)?;
idExprs		:	idExpr (COMMA idExpr)* COMMA?;

// TODO: Prop - get,set,refget
typedIdExprs:	typeSpec idExprs SEMI;

attrib		:	id	(	'=' idOrLit
					|	'(' idOrLit (COMMA idOrLit)* ')'
					)?;
attribBlk	:	LBRACK	attrib (COMMA attrib)* COMMA? RBRACK;

caseStmt	:	CASE expr (COMMA expr)* COLON levStmt+ (FALL SEMI)?;

initList	:	COLON id funcCall (COMMA id funcCall)* COMMA?;
ctorDef		:	funcTypeDef	initList?	(levStmt|SEMI);

funcDef		:	id tplParams?	funcTypeDef (RARROW typeSpec)?	(levStmt|'=>' expr SEMI);
opDef		:	STRING_LIT		funcTypeDef (RARROW typeSpec)?	(levStmt|'=>' expr SEMI);

/*
stmt		:	stmtDef								# StmtDecl
			|	RETURN	expr?	SEMI				# ReturnStmt
			|	THROW	expr	SEMI				# ThrowStmt
			|	BREAK	INTEGER_LIT	SEMI			# BreakStmt
			|	IF	LPAREN expr RPAREN stmt
					( ELSE stmt )?					# IfStmt
			|	SWITCH LPAREN expr RPAREN	LCURLY
				caseStmt+ 	(ELSE stmt+)?	RCURLY	# SwitchStmt
			|	LOOP	stmt						# LoopStmt
			|	FOR LPAREN stmtDef expr SEMI expr RPAREN stmt
					( ELSE stmt )?					# ForStmt
			|	WHILE LPAREN expr RPAREN stmt
					( ELSE stmt )?					# WhileStmt
			|	DO stmt WHILE LPAREN expr RPAREN SEMI?	# DoWhileStmt
			|	expr TIMES			id?	stmt		# TimesStmt
			|	expr DBL_POINT expr id?	stmt		# EachStmt
			| 	(expr	assignOP)+		expr SEMI	# AssignmentStmt
			| 	expr	aggrAssignOP	expr SEMI	# AssignmentStmt
			|	LCURLY	stmt*	RCURLY				# BlockStmt
			|	expr SEMI							# ExpressionStmt
			;

// in and out of class, in static, in stmt
stmtDef		:	USING	nestedType (COMMA nestedType)*	SEMI	# Using
			|	VAR		typedIdExprs				# VariableDecl
			|	CONST	typedIdExprs				# VariableDecl
			;

// in class and in static
//inClass
classExtDef	:	v=(PUB | PRIV | PROT) COLON			# AccessMod
			|	FIELD LCURLY typedIdExprs* RCURLY	# FieldDecl
			|	FIELD		typedIdExprs			# FieldDecl
			|	PROP		typedIdExprs			# PropDecl
			|	METH		funcDef					# MethDecl
			|	OPERATOR	opDef					# OpDecl
			;
// in class
//inUnstaticClass
classDef	:	CTOR	ctorDef						# CtorDecl
			|	ALIAS 	id ASSIGN typeSpec SEMI		# Alias
			|	STATIC	LCURLY	classExtDef* RCURLY	# StaticDecl
			|	STATIC			classExtDef			# StaticDecl
			|	classExtDef							# ClassExtendedDecl
			|	nestedLevel							# ClassNested
			;

// in and out of class
//inAnywhere
nestedLevel	:	LBRACK	attrib (COMMA attrib)*	RBRACK			# Attributes
			|	v=(CLASS|STRUCT|UNION)	id tplParams?
				LCURLY	classDef*	RCURLY						# ClassDecl
			|	ENUM	id	LCURLY	idExpr (COMMA idExpr)* COMMA? RCURLY	# EnumDecl
			|	FUNC	funcDef									# FunctionDecl
			|	stmtDef											# restStmt
			;
// out of class
//inToplevel
topLevel	:	NS	id (SCOPE id)*	SEMI						# Namespace
			|	NS	id (SCOPE id)*	LCURLY	topLevel+	RCURLY	# Namespace
			|	nestedLevel										# NestedDecl
			;

prog		:	topLevel+;
*/

prog		:	levTop+;

// only refer to these lev*, not the in*
levTop		:	attribBlk? ( inAnyStmt | inAnyDecl | inTop );
levClass	:	attribBlk? ( inAnyStmt | inAnyDecl | inClass | inUnstatic );
levStatic	:	attribBlk? ( inAnyStmt | inAnyDecl | inClass );
levStmt		:	attribBlk? ( inAnyStmt | inStmt );
levStmtDef	:	attribBlk? ( inAnyStmt );

// ns
inTop		:	NS	id (SCOPE id)*	SEMI					# Namespace
			|	NS	id (SCOPE id)*	LCURLY	levTop+	RCURLY	# Namespace
			;

// attrib, using, var, const
inAnyStmt	:	USING	nestedType (COMMA nestedType)* SEMI	# Using
			|	VAR		LCURLY	typedIdExprs* RCURLY		# VariableDecl
			|	VAR				typedIdExprs				# VariableDecl
			|	CONST	LCURLY	typedIdExprs* RCURLY		# VariableDecl
			|	CONST			typedIdExprs				# VariableDecl
			;

// class, enum, func
inAnyDecl	:	v=(CLASS|STRUCT|UNION) id tplParams?
						LCURLY	levClass*	RCURLY	# ClassDecl
			|	ENUM id	LCURLY	idExprs		RCURLY	# EnumDecl
			|	FUNC	LCURLY	funcDef*	RCURLY	# FunctionDecl
			|	FUNC			funcDef				# FunctionDecl
			|	OPERATOR		opDef				# OpDecl
			;

// ppp, field, prop, meth, op
inClass		:	v=(PUB | PRIV | PROT) COLON			# AccessMod
			|	PROP		typedIdExprs			# PropDecl
		//	|	FIELD LCURLY typedIdExprs* RCURLY	# FieldDecl
		//	|	FIELD		typedIdExprs			# FieldDecl
		//	|	METH		funcDef					# MethDecl
			;

// ctor, alias, static
inUnstatic	:	CTOR	ctorDef						# CtorDecl
			|	DTOR	ctorDef						# CtorDecl
			|	ALIAS 	id ASSIGN typeSpec SEMI		# Alias
			|	STATIC	LCURLY	levStatic* RCURLY	# StaticDecl
			|	STATIC			levStatic			# StaticDecl
			;

inStmt		:	RETURN	expr?	SEMI				# ReturnStmt
			|	THROW	expr	SEMI				# ThrowStmt
			|	BREAK	INTEGER_LIT	SEMI			# BreakStmt
			|	IF	LPAREN expr RPAREN levStmt
				(ELSE levStmt)?						# IfStmt
			|	SWITCH LPAREN expr RPAREN	LCURLY
				caseStmt+ 	(ELSE levStmt+)? RCURLY	# SwitchStmt
			|	LOOP	levStmt						# LoopStmt
			|	FOR LPAREN levStmtDef expr SEMI expr RPAREN
				levStmt
				(ELSE levStmt)?						# ForStmt
			|	WHILE LPAREN expr RPAREN
				levStmt
				(ELSE levStmt)?						# WhileStmt
			|	DO levStmt
				WHILE LPAREN expr RPAREN SEMI?		# DoWhileStmt
			|	expr TIMES			id?	levStmt		# TimesStmt
			|	expr DBL_POINT expr id?	levStmt		# EachStmt
			| 	(expr	assignOP)+		expr SEMI	# MultiAssignStmt
			| 	expr	aggrAssignOP	expr SEMI	# AggrAssignStmt
			|	LCURLY	levStmt*	RCURLY			# BlockStmt
			|	expr SEMI							# ExpressionStmt
			;
