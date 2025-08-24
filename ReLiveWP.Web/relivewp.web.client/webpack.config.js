const path = require('path');
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const HtmlWebpackPlugin = require('html-webpack-plugin');
const FaviconsWebpackPlugin = require('favicons-webpack-plugin')
const { env } = require('process');

module.exports = [
    {
        entry: {
            "index": "./src/index.tsx",
        },
        target: "web",
        mode: env.NODE_ENV || "development",
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
                        "sass-loader",
                    ],
                },
                {
                    test: /\.(png|jpg|gif|webp|avif)$/i,
                    use: [
                        { loader: 'url-loader', options: { limit: 4096, fallback: { loader: 'file-loader', options: { outputPath: 'static/' } } } },
                    ],
                },
                {
                    test: /\.(woff(2)?|ttf|eot|wasm|svg)(\?v=\d+\.\d+\.\d+)?$/i,
                    use: [
                        { loader: 'file-loader', options: { outputPath: 'static/' } }
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
                '~': path.resolve(__dirname, "src/")
            }
        },
        plugins: [
            new MiniCssExtractPlugin({
                filename: env.NODE_ENV === 'production' ? "[name].[chunkhash].css" : "[name].bundle.css",
                chunkFilename: env.NODE_ENV === 'production' ? "[id].bundle.[chunkhash].css" : "[id].bundle.css"
            }),
            new HtmlWebpackPlugin({
                inject: true,
                template: "./src/index.html",
                chunks: ["index"],
                filename: "index.html",
                publicPath: "/"
            }),
            // new FaviconsWebpackPlugin({
            //     logo: './static/wam-circular.png',
            //     favicons: {
            //         icons: { android: false, appleIcon: false, appleStartup: false, windows: false, yandex: false, }
            //     }
            // })
        ],
        output: {
            filename: env.NODE_ENV === 'production' ? '[name].[chunkhash].js' : '[name].bundle.js',
            chunkFilename: env.NODE_ENV === 'production' ? '[id].bundle.[chunkhash].js' : '[id].bundle.js',
            path: path.resolve(__dirname, 'dist'),
        },
        devServer: {
            historyApiFallback: true,
            allowedHosts: [
                'int.relivewp.net'
            ]
        },
    }];