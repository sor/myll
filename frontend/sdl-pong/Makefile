# SDL3 Pong sample for Myll.
# Run from this directory:
#   make
#   ./gen/pong

MYLL_ROOT ?= ../..
MYLL      := dotnet run --project $(MYLL_ROOT)/frontend -- -i '*.myll' -o gen

CXX       ?= g++
CXXFLAGS  ?= -std=c++23 -O2 -Wall -Wextra
SDL_CFLAGS = $(shell pkg-config --cflags sdl3)
SDL_LIBS   = $(shell pkg-config --libs sdl3)

.PHONY: all clean run

all: gen/pong

gen/.stamp: $(wildcard *.myll)
	@mkdir -p gen
	$(MYLL)
	@touch gen/.stamp

gen/pong: gen/.stamp
	$(CXX) $(CXXFLAGS) $(SDL_CFLAGS) gen/pong.cpp -o $@ $(SDL_LIBS)

run: gen/pong
	./gen/pong

clean:
	rm -rf gen
