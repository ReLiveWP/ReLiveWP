const path = require('path');
const CopyPlugin = require('copy-webpack-plugin');
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const HtmlWebpackPlugin = require('html-webpack-plugin');
const { InjectManifest } = require('workbox-webpack-plugin');
const { env } = require('process');

const mode = env.NODE_ENV || "production";

// --mode on the command line beats the value in this config, so anything that has to know
// which kind of build this really is has to read it back off argv
module.exports = (_env, argv) => {
    const production = (argv && argv.mode ? argv.mode : mode) === "production";

    return [{
        entry: {
            "index": "./src/index.tsx",
        },
        target: "web",
        mode,
        devtool: 'source-map',
        module: {
            rules: [
                {
                    test: /\.tsx?$/,
                    use: ['ts-loader'],
                    exclude: /node_modules/,
                },
                {
                    test: /\.css$/i,
                    use: [
                        MiniCssExtractPlugin.loader,
                        { loader: 'css-loader', options: { importLoaders: 1 } },
                    ]
                },
                {
                    test: /\.scss$/i,
                    use: [
                        MiniCssExtractPlugin.loader,
                        { loader: 'css-loader', options: { importLoaders: 1 } },
                        {
                            loader: "sass-loader",
                            options: {
                                sassOptions: {
                                    loadPaths: [path.resolve(__dirname, '../packages/ui/styles')]
                                }
                            }
                        },
                    ],
                },
                {
                    test: /\.(png|jpg|gif|webp|avif)$/i,
                    use: [
                        { loader: 'url-loader', options: { limit: 4096, fallback: { loader: 'file-loader', options: { outputPath: 'static/' } } } },
                    ],
                },
                {
                    test: /\.(woff(2)?|ttf|eot|wasm)(\?v=\d+\.\d+\.\d+)?$/i,
                    use: [
                        { loader: 'file-loader', options: { outputPath: 'static/' } }
                    ]
                },
                {
                    test: /\.svg$/i,
                    oneOf: [
                        {
                            resourceQuery: /url/,
                            type: 'asset/resource',
                            generator: { filename: 'static/[name].[contenthash][ext]' }
                        },
                        {
                            issuer: /\.[jt]sx?$/,
                            use: [
                                {
                                    loader: '@svgr/webpack',
                                    options: {
                                        svgProps: { fill: 'currentColor' },
                                        svgoConfig: {
                                            plugins: [
                                                { name: 'preset-default', params: { overrides: { removeViewBox: false } } },
                                                'removeDimensions',
                                                'convertStyleToAttrs'
                                            ]
                                        }
                                    }
                                }
                            ]
                        },
                        {
                            type: 'asset/resource',
                            generator: { filename: 'static/[name].[contenthash][ext]' }
                        }
                    ]
                }
            ],
        },
        optimization: {
            runtimeChunk: 'single',
            usedExports: true,
            splitChunks: {
                chunks: "all",
                minSize: 4096
            }
        },
        resolve: {
            extensions: ['.tsx', '.ts', '.js'],
            alias: {
                '~': path.resolve(__dirname, "src/"),
            }
        },
        plugins: [
            new MiniCssExtractPlugin({
                filename: mode === 'production' ? "[name].[chunkhash].css" : "[name].bundle.css",
                chunkFilename: mode === 'production' ? "[id].bundle.[chunkhash].css" : "[id].bundle.css"
            }),
            new HtmlWebpackPlugin({
                inject: true,
                template: "./src/index.html",
                chunks: ["index"],
                filename: "index.html",
                publicPath: "/"
            }),
            new CopyPlugin({
                patterns: [{ from: "static", to: "." }]
            }),
            // dev builds never emit a worker, a stale precache is not worth the debugging
            ...(production ? [new InjectManifest({
                swSrc: "./src/service-worker.js",
                swDest: "sw.js",
                exclude: [/\.map$/, /^sw\.js$/],
                maximumFileSizeToCacheInBytes: 5 * 1024 * 1024,
            })] : []),
        ],
        output: {
            filename: mode === 'production' ? '[name].[chunkhash].js' : '[name].bundle.js',
            chunkFilename: mode === 'production' ? '[id].bundle.[chunkhash].js' : '[id].bundle.js',
            path: path.resolve(__dirname, 'dist'),
            clean: true,
        },
        devServer: {
            historyApiFallback: true,
            allowedHosts: [
                'int.relivewp.net',
                'mail.int.relivewp.net'
            ]
        },
    }];
};
