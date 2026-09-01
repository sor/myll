# Build all Myll Unix utilities in this directory.
#
# Run from this directory:
#   make
#   make clean

MYLL_ROOT ?= ../../..
MYLL      := dotnet run --project $(MYLL_ROOT)/frontend -- -i '*.myll' -o gen

CXX      ?= g++
CXXFLAGS ?= -std=c++20 -I. -Ithird_party -DCXXOPTS_NO_REGEX -O1 -Wsuggest-override

RELEASE_FLAGS ?= -O2 -s
DEBUG_FLAGS   ?= -Og -g

TOOLS    := $(basename $(wildcard *.myll))
BINS     := $(addprefix gen/,$(TOOLS))

.PHONY: all clean release debug

all: $(BINS)

# Generate C++ sources for all .myll files in one Myll invocation.
gen/.stamp: $(wildcard *.myll)
	@mkdir -p gen
	$(MYLL)
	@touch gen/.stamp

# Compile each generated .cpp into its own binary.
$(BINS): gen/.stamp
	$(CXX) $(CXXFLAGS) gen/$(@F).cpp -o $@

release:
	$(MAKE) CXXFLAGS="$(CXXFLAGS) $(RELEASE_FLAGS)"

debug:
	$(MAKE) CXXFLAGS="$(CXXFLAGS) $(DEBUG_FLAGS)"

clean:
	rm -rf gen
