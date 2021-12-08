parser grammar MyllParser;

options { tokenVocab = MyllLexer; }

comment		:	COMMENT;

// =!= all operators handled by ToOp except those mentioned =!=
postOP		:	v=(	'++' | '--' );

// handled by ToPreOp because of collisions
preOP		:	v=(	'++' | '--' | '+' | '-' | '!' | '~' | '*' | '&' );

powOP		:		'*''*';
multOP		:	v=(	'*' | '/' | '%' | '&' | '·' | '×' | '÷' );
addOP		:	v=(	'+' | '-' | '^' | '|' ); // split xor and or?
shiftOP		:		'<<' | '>''>';

cmpOp       :   	'<=>';
relOP	:	v=(	'<=' | '>=' | '<' | '>' );
equalOP	:	v=(	'==' | '!=' );

andOP		:		'&&';
orOP		:		'||';

nulCoalOP	:		'?:';

memAccOP	:	v=(	'.'  | '?.'  | '->'  );
memAccPtrOP	:	v=(	'.*' | '?.*' | '->*' );
//memAccOP	:	v=(	'.'  | '..'  | '?.'  | '?..'  | '->'  );
//memAccPtrOP	:	v=(	'.*' | '..*' | '?.*' | '?..*' | '->*' );

// handled by ToAssignOp because of collisions
assignOP	:		'=';
aggrAssignOP:	v=(	'**=' | '*=' | '/=' | '%=' | '&=' |	'·=' |	'×=' |	'÷=' |
					'+='  | '-=' | '|=' | '^=' | '<<=' | '>>=' );

lit			:	CLASS_LIT | HEX_LIT | OCT_LIT | BIN_LIT | INTEGER_LIT | FLOAT_LIT | STRING_LIT | CHAR_LIT | BOOL_LIT | NUL;
wildId		:	AUTOINDEX | USCORE;
id			:	ID;
idOrLit		:	id | lit;

// +++ handled
specialType	:	v=( AUTO | VOID | BOOL );
charType	:	v=( CHAR | CODEPOINT | STRING );
floatingType:	v=( FLOAT | F128 | F64 | F32 | F16 ); // 80 and 96 bit?
binaryType	:	v=( BYTE | B64 | B32 | B16 | B8 );
signedIntType:	v=( INT  | ISIZE | I64 | I32 | I16 | I8 );
unsignIntType: 	v=( UINT | USIZE | U64 | U32 | U16 | U8 );

qual		:	v=( CONST | MUTABLE | VOLATILE | STABLE );

typePtr		:	qual*
				(	ptr=( DBL_AMP | AMP | STAR | PTR_TO_ARY )
				|	ary=( AT_LBRACK | LBRACK ) expr? RBRACK )
				suffix=( EM | PLUS | QM )?;

idTplArgs	:	id	tplArgs?;

typespec	:	qual*
				(	typespecBasic	typePtr*
				|	FUNC			typePtr*	typespecFunc
				|	typespecNested	typePtr*);

typespecBasic	:	specialType
				|	charType
				|	floatingType
				|	binaryType
				|	signedIntType
				|	unsignIntType;

typespecFunc	:	funcTypeDef (RARROW typespec)?;

// TODO different order than ScopedExpr
typespecNested	:	idTplArgs	(SCOPE idTplArgs)*
								(SCOPE v=( CTOR | DTOR | COPYCTOR | MOVECTOR ))?;
typespecsNested	:	typespecNested (COMMA typespecNested)* COMMA?;

// --- handled

arg			:	(id COLON)? expr;
args		:	arg (COMMA arg)* COMMA?;
funcCall	:	ary=( QM_LPAREN | LPAREN )	args?	RPAREN;
indexCall	:	ary=( QM_LBRACK | LBRACK )	args	RBRACK;

param		:	typespec id?;
funcTypeDef	:	LPAREN (param (COMMA param)* COMMA?)? RPAREN;

// can't contain expr, will fck up through idTplArgs with multiple templates (e.g. op | from enums)
tplArg		:	lit | typespec;
tplArgs		:	LT tplArg	(COMMA tplArg)*	COMMA? GT;
tplParams	:	LT id		(COMMA id)*		COMMA? GT;

threeWay	:	(relOP | equalOP)	COLON	expr;

// Tier 3
//cast: nothing = static, ? = dynamic, ! = const & reinterpret
// TODO REMOVE THEM ALL, IN CODE TOO
//					xxx
//preOpExpr	:	preOP;
//castExpr	:	(MOVE|LPAREN (QM|EM)? typespec RPAREN);
//sizeofExpr	:	SIZEOF;
//deleteExpr	:	DELETE	(ary='['']')?;

// The order here is significant, it determines the operator precedence
expr		:	(idTplArgs	SCOPE)+	idTplArgs	# ScopedExpr
			|	expr
					(	postOP
					|	funcCall
					|	indexCall
					|	memAccOP	idTplArgs )	# PostExpr
			| // can not even be associative without a contained expr
				NEW	typespec?	funcCall?		# NewExpr
			| // inherently <assoc=right>, so no need to tell ANTLR
				(	(	FORWARD // parens included
					|	MOVE    // parens included
					|	LPAREN (QM|EM)? typespec RPAREN)
				|	SIZEOF
				|	DELETE	( ary='['']' )?
				|	preOP
				)						expr	# PreExpr // this visitor is unused
			|	expr	memAccPtrOP		expr	# MemPtrExpr
			| <assoc=right>
				expr	powOP			expr	# PowExpr
			|	expr	multOP			expr	# MultExpr
			|	expr	addOP			expr	# AddExpr
			|	expr	shiftOP			expr	# ShiftExpr
			|	expr	cmpOp			expr	# ComparisonExpr
			|	expr	relOP			expr	# RelationExpr
			|	expr	equalOP			expr	# EqualityExpr
			|	expr	andOP			expr	# AndExpr
			|	expr	orOP			expr	# OrExpr
			|	expr	TRP_POINT		expr	# RangeExpr
			|	expr	nulCoalOP		expr	# NullCoalesceExpr
			// TODO: 2-way-cond and throw were in the same level, test if this still works fine
			| <assoc=right>
				expr	QM expr COLON	expr	# ConditionalExpr
			| <assoc=right>
				expr	DBL_QM threeWay+ (COLON expr)?	# ThreeWayConditionalExpr
			| <assoc=right>
				THROW	expr					# ThrowExpr		// Good or not?
			|	LPAREN	expr	RPAREN			# ParenExpr
			|	wildId							# WildIdExpr
			|	FUNC funcTypeDef
				(RARROW typespec)?	funcBody	# LambdaExpr	// TODO CST
			|	lit								# LiteralExpr	// TODO
			|	idTplArgs						# IdTplExpr
			;

idAccessor	:	id	(LCURLY accessorDef+ RCURLY)?	(ASSIGN expr)?;
idExpr		:	id									(ASSIGN expr)?;
idAccessors	:	idAccessor	(COMMA idAccessor)*	COMMA?;
idExprs		:	idExpr		(COMMA idExpr)*		COMMA?;
typedIdAcors:	typespec 	idAccessors	SEMI;

attribId	:	id | CONST | FALL | THROW;
attrib		:	attribId
				(	'=' idOrLit
				|	'(' idOrLit (COMMA idOrLit)* COMMA? ')'
				)?;
attribBlk	:	LBRACK	attrib (COMMA attrib)* COMMA? RBRACK;

caseStmt	:	CASE expr (COMMA expr)*
				(	COLON		levStmt+ (FALL SEMI)?
				|	LCURLY		levStmt* (FALL SEMI)? RCURLY
				|	PHATRARROW	levStmt  (FALL SEMI)?);

defaultStmt	:	(ELSE|DEFAULT)
				(	COLON		levStmt+
				|	LCURLY		levStmt* RCURLY
				|	PHATRARROW	levStmt);

initList	:	COLON id funcCall (COMMA id funcCall)* COMMA?;
// TODO: initList needs to support ": ctor()"

// is just SEMI as well in levStmt->inStmt
funcBody	:	PHATRARROW expr SEMI
			|	levStmt;
accessorDef	:	attribBlk?	qual* v=( GET | REFGET | SET ) funcBody;
funcDef		:	id			tplParams?	funcTypeDef (RARROW typespec)?
				(REQUIRES typespecsNested)?		// TODO
				funcBody;
opDef		:	STRING_LIT	tplParams?	funcTypeDef (RARROW typespec)?
				(REQUIRES typespecsNested)?		// TODO
				funcBody;
// TODO: can be used in more places
condThen	:	LPAREN expr RPAREN	levStmt;

// DON'T refer to these in* here, ONLY refer to lev* levels
// ns, class, enum, func, ppp, c/dtor, alias, static
inDecl		:	NS id (SCOPE id)* SEMI						# Namespace // or better COLON
			|	NS id (SCOPE id)* LCURLY levDecl+ RCURLY	# Namespace
			|	v=( STRUCT | CLASS | UNION ) id tplParams?
				(COLON		bases=typespecsNested)?
				(REQUIRES 	reqs=typespecsNested)?	// TODO
						LCURLY	levDecl*	RCURLY	# StructDecl
			|	CONCEPT	id tplParams?		// TODO
				(COLON	typespecsNested)?
						LCURLY	levDecl*	RCURLY	# ConceptDecl
			// TODO aspect
			|	ENUM	id
				(COLON	bases=typespecBasic)?	// TODO: enum inheritance
				LCURLY	idExprs		RCURLY			# EnumDecl
			|	v=( FUNC | PROC | METHOD )
				(	LCURLY	funcDef*	RCURLY
				|			funcDef				)	# FunctionDecl
			|	OPERATOR
				(	LCURLY	opDef*		RCURLY
				|			opDef				)	# OpDecl
			|	USING		typespecsNested	SEMI	# UsingDecl
			// TODO: alias needs template support
			|	ALIAS id ASSIGN	typespec 	SEMI	# AliasDecl
			|	v=( VAR | FIELD | CONST | LET )
				(	LCURLY	typedIdAcors*	RCURLY
				|			typedIdAcors		)	# VariableDecl
// class only:
			|	v=( CTOR | COPYCTOR | MOVECTOR )
				funcTypeDef	initList?		(SEMI | levStmt) # CtorDecl
			|	DTOR	(LPAREN RPAREN)?	(SEMI | levStmt) # DtorDecl
			;

inStmt		:	SEMI								# EmptyStmt
			|	LCURLY	levStmt*	RCURLY			# BlockStmt
			|	USING		typespecsNested	SEMI	# UsingStmt
			// TODO: alias needs template support
			|	ALIAS id ASSIGN	typespec 	SEMI	# AliasStmt
			|	v=( VAR | FIELD | CONST | LET )
				(	LCURLY	typedIdAcors*	RCURLY
				|			typedIdAcors		)	# VariableStmt
			|	RETURN	expr?		SEMI			# ReturnStmt
			|	DO RETURN expr?
				IF LPAREN expr RPAREN	SEMI		# ReturnIfStmt
			|	THROW	expr			SEMI		# ThrowStmt
			|	BREAK	INTEGER_LIT?	SEMI		# BreakStmt
			|	IF			condThen
				(ELSE IF	condThen)*	// helps with formatting properly and de-nesting the AST
				(ELSE		levStmt)?				# IfStmt
			|	SWITCH	LPAREN expr RPAREN	LCURLY
				caseStmt+ 	defaultStmt?	RCURLY	# SwitchStmt
			|	LOOP	levStmt						# LoopStmt
			|	FOR LPAREN levStmt	// TODO: add the syntax: for( a : b )
					expr SEMI
					expr RPAREN	levStmt
				(ELSE			levStmt)?			# ForStmt
			|	WHILE		condThen
				(ELSE		levStmt)?				# WhileStmt
			|	DO		levStmt
				WHILE	LPAREN expr RPAREN			# DoWhileStmt
			|	DO expr TIMES		id?	levStmt		# TimesStmt
			|	TRY		levStmt
				(CATCH	funcTypeDef		levStmt)+	# TryCatchStmt		// TODO CST
			|	DEFER	levStmt						# DeferStmt			// TODO CST, scope_exit(,fail,success)
													// #include <experimental/scope>
													// std::experimental::scope_exit guard([]{ cout << "Exit!" << endl; });
			| 	expr	(assignOP		expr)+ SEMI	# MultiAssignStmt
			| 	expr	aggrAssignOP	expr SEMI	# AggrAssignStmt
			|	expr	SEMI						# ExpressionStmt
			;

// ONLY refer to these lev* levels, NOT the in*
levDecl		:	attribBlk	LCURLY	levDecl+ RCURLY	# AttribDeclBlock // must be in here, since it MUST have an attrib block
			|	attribBlk	COLON 					# AttribState     // everything needs an antonym to make this work
			|	attribBlk?	inDecl					# AttribDecl;
levStmt		:	attribBlk?	inStmt					# AttribStmt;

module		:	MODULE id SEMI;
imports		:	IMPORT id (COMMA id)* COMMA? SEMI;

prog		:	module? imports* levDecl+;
